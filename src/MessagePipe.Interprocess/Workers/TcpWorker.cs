using MessagePack;
using MessagePipe.Interprocess.Internal;
#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
#endif
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MessagePipe.Interprocess.Workers
{
    [Preserve]
    public sealed class TcpWorker : IDisposable
    {
        readonly IServiceProvider provider;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly IAsyncPublisher<IInterprocessKey, IInterprocessValue> publisher;
        readonly MessagePipeInterprocessOptions options;

        // Channel is used from publisher for thread safety of write packet
        int initializedServer = 0;
        Lazy<SocketTcpServer> server;
        Channel<byte[]> channel;

        int initializedClient = 0;
        Lazy<SocketTcpClient> client;

        // request-response
        int messageId = 0;
        ConcurrentDictionary<int, TaskCompletionSource<IInterprocessValue>> responseCompletions = new ConcurrentDictionary<int, TaskCompletionSource<IInterprocessValue>>();

        // create from DI
        [Preserve]
        public TcpWorker(IServiceProvider provider, MessagePipeInterprocessTcpOptions options, IAsyncPublisher<IInterprocessKey, IInterprocessValue> publisher)
        {
            this.provider = provider;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.options = options;
            this.publisher = publisher;

            this.server = new Lazy<SocketTcpServer>(() =>
            {
                return SocketTcpServer.Listen(options.Host, options.Port);
            });

            this.client = new Lazy<SocketTcpClient>(() =>
            {
                return SocketTcpClient.Connect(options.Host, options.Port);
            });

#if !UNITY_2018_3_OR_NEWER
            this.channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = true
            });
#else
            this.channel = Channel.CreateSingleConsumerUnbounded<byte[]>();
#endif

            if (options.HostAsServer != null && options.HostAsServer.Value)
            {
                StartReceiver();
            }
        }
#if NET5_0_OR_GREATER
        [Preserve]
        public TcpWorker(IServiceProvider provider, MessagePipeInterprocessTcpUdsOptions options, IAsyncPublisher<IInterprocessKey, IInterprocessValue> publisher)
        {
            this.provider = provider;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.options = options;
            this.publisher = publisher;

            this.server = new Lazy<SocketTcpServer>(() =>
            {
                return SocketTcpServer.ListenUds(options.SocketPath);
            });

            this.client = new Lazy<SocketTcpClient>(() =>
            {
                return SocketTcpClient.ConnectUds(options.SocketPath);
            });

#if !UNITY_2018_3_OR_NEWER
            this.channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = true
            });
#else
            this.channel = Channel.CreateSingleConsumerUnbounded<byte[]>();
#endif

            if (options.HostAsServer != null && options.HostAsServer.Value)
            {
                StartReceiver();
            }
        }
#endif
        public void Publish<TKey, TMessage>(TKey key, TMessage message)
        {
            if (Interlocked.Increment(ref initializedClient) == 1) // first incr, channel not yet started
            {
                _ = client.Value; // init
                RunPublishLoop();
            }

            var buffer = MessageBuilder.BuildPubSubMessage(key, message, options.MessagePackSerializerOptions);
            channel.Writer.TryWrite(buffer);
        }

        public async ValueTask<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref initializedClient) == 1) // first incr, channel not yet started
            {
                _ = client.Value; // init
                RunPublishLoop();
            }

            var mid = Interlocked.Increment(ref messageId);
            var tcs = new TaskCompletionSource<IInterprocessValue>();
            responseCompletions[mid] = tcs;
            var buffer = MessageBuilder.BuildRemoteRequestMessage(typeof(TRequest), typeof(TResponse), mid, request, options.MessagePackSerializerOptions);
            channel.Writer.TryWrite(buffer);
            var memoryValue = await tcs.Task.ConfigureAwait(false);
            return MessagePackSerializer.Deserialize<TResponse>(memoryValue.ValueMemory, options.MessagePackSerializerOptions);
        }

        // Send packet to tcp socket from publisher
        async void RunPublishLoop()
        {
            var reader = channel.Reader;
            var token = cancellationTokenSource.Token;
            var tcpClient = client.Value;
            RunReceiveLoop(tcpClient); // also setup receive loop

            while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    try
                    {
                        await tcpClient.SendAsync(item, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException) return;
                        if (token.IsCancellationRequested) return;

                        // network error, terminate.
                        options.UnhandledErrorHandler("network error, publish loop will terminate." + Environment.NewLine, ex);
                        return;
                    }
                }
            }
        }

        public void StartReceiver()
        {
            if (Interlocked.Increment(ref initializedServer) == 1) // first incr, channel not yet started
            {
                var s = server.Value; // init
                s.StartAcceptLoopAsync(RunReceiveLoop, cancellationTokenSource.Token);
            }
        }

        // Receive from tcp socket and push value to subscribers.
        async void RunReceiveLoop(SocketTcpClient client)
        {
            var token = cancellationTokenSource.Token;
            var buffer = new byte[65536];
            ReadOnlyMemory<byte> readBuffer = Array.Empty<byte>();
            while (!token.IsCancellationRequested)
            {
                ReadOnlyMemory<byte> value = Array.Empty<byte>();
                try
                {
                    if (readBuffer.Length == 0)
                    {
                        var readLen = await client.ReceiveAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                        if (readLen == 0) return; // end of stream(disconnect)
                        readBuffer = buffer.AsMemory(0, readLen);
                    }
                    else if (readBuffer.Length < 4) // rare case
                    {
                        var readLen = await client.ReceiveAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                        if (readLen == 0) return;
                        var newBuffer = new byte[readBuffer.Length + readLen];
                        readBuffer.CopyTo(newBuffer);
                        buffer.AsSpan(readLen).CopyTo(newBuffer.AsSpan(readBuffer.Length));
                        readBuffer = newBuffer;
                    }

                    var messageLen = MessageBuilder.FetchMessageLength(readBuffer.Span);
                    if (readBuffer.Length == (messageLen + 4)) // just size
                    {
                        value = readBuffer.Slice(4, messageLen); // skip length header
                        readBuffer = Array.Empty<byte>();
                        goto PARSE_MESSAGE;
                    }
                    else if (readBuffer.Length > (messageLen + 4)) // over size
                    {
                        value = readBuffer.Slice(4, messageLen);
                        readBuffer = readBuffer.Slice(messageLen + 4);
                        goto PARSE_MESSAGE;
                    }
                    else // needs to read more
                    {
                        var readLen = readBuffer.Length;
                        if (readLen < (messageLen + 4))
                        {
                            if (readBuffer.Length != buffer.Length)
                            {
                                var newBuffer = new byte[buffer.Length];
                                readBuffer.CopyTo(newBuffer);
                                buffer = newBuffer;
                            }

                            if (buffer.Length < messageLen + 4)
                            {
                                Array.Resize(ref buffer, messageLen + 4);
                            }
                        }
                        var remain = messageLen - (readLen - 4);
                        await ReadFullyAsync(buffer, client, readLen, remain, token).ConfigureAwait(false);
                        value = buffer.AsMemory(4, messageLen);
                        readBuffer = Array.Empty<byte>();
                        goto PARSE_MESSAGE;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) return;
                    if (token.IsCancellationRequested) return;

                    // network error, terminate.
                    options.UnhandledErrorHandler("network error, receive loop will terminate." + Environment.NewLine, ex);
                    return;
                }
            PARSE_MESSAGE:
                try
                {
                    var message = MessageBuilder.ReadPubSubMessage(value.ToArray()); // can avoid copy?
                    switch (message.MessageType)
                    {
                        case MessageType.PubSub:
                            publisher.Publish(message, message, CancellationToken.None);
                            break;
                        case MessageType.RemoteRequest:
                            {
                                // NOTE: should use without reflection(Expression.Compile)
                                var header = Deserialize<RequestHeader>(message.KeyMemory, options.MessagePackSerializerOptions);
                                var (mid, reqTypeName, resTypeName) = (header.MessageId, header.RequestType, header.ResponseType);
                                byte[] resultBytes;
                                try
                                {
                                    var t = AsyncRequestHandlerRegistory.Get(reqTypeName, resTypeName);
                                    var interfaceType = t.GetInterfaces().Where(x => x.IsGenericType && x.Name.StartsWith("IAsyncRequestHandler"))
                                        .First(x => x.GetGenericArguments().Any(y => y.FullName == header.RequestType));
                                    var coreInterfaceType = t.GetInterfaces().Where(x => x.IsGenericType && x.Name.StartsWith("IAsyncRequestHandlerCore"))
                                        .First(x => x.GetGenericArguments().Any(y => y.FullName == header.RequestType));
                                    var service = provider.GetRequiredService(interfaceType); // IAsyncRequestHandler<TRequest,TResponse>
                                    var genericArgs = interfaceType.GetGenericArguments(); // [TRequest, TResponse]
                                    // Unity IL2CPP does not work(can not invoke nongenerics MessagePackSerializer)
                                    var request = MessagePackSerializer.Deserialize(genericArgs[0], message.ValueMemory, options.MessagePackSerializerOptions);
                                    var responseTask = coreInterfaceType.GetMethod("InvokeAsync")!.Invoke(service, new[] { request, CancellationToken.None });
#if !UNITY_2018_3_OR_NEWER
                                    var task = typeof(ValueTask<>).MakeGenericType(genericArgs[1]).GetMethod("AsTask")!.Invoke(responseTask, null);
#else
                                    var asTask = typeof(UniTaskExtensions).GetMethods().First(x => x.IsGenericMethod && x.Name == "AsTask")
                                        .MakeGenericMethod(genericArgs[1]);
                                    var task = asTask.Invoke(null, new[] { responseTask });
#endif
                                    await ((System.Threading.Tasks.Task)task!); // Task<T> -> Task
                                    var result = task.GetType().GetProperty("Result")!.GetValue(task);
                                    resultBytes = MessageBuilder.BuildRemoteResponseMessage(mid, genericArgs[1], result!, options.MessagePackSerializerOptions);
                                }
                                catch (Exception ex)
                                {
                                    // NOTE: ok to send stacktrace?
                                    resultBytes = MessageBuilder.BuildRemoteResponseError(mid, ex.ToString(), options.MessagePackSerializerOptions);
                                }

                                await client.SendAsync(resultBytes).ConfigureAwait(false);
                            }
                            break;
                        case MessageType.RemoteResponse:
                        case MessageType.RemoteError:
                            {
                                var mid = Deserialize<int>(message.KeyMemory, options.MessagePackSerializerOptions);
                                if (responseCompletions.TryRemove(mid, out var tcs))
                                {
                                    if (message.MessageType == MessageType.RemoteResponse)
                                    {
                                        tcs.TrySetResult(message); // synchronous completion, use memory buffer immediately.
                                    }
                                    else
                                    {
                                        var errorMsg = MessagePackSerializer.Deserialize<string>(message.ValueMemory, options.MessagePackSerializerOptions);
                                        tcs.TrySetException(new RemoteRequestException(errorMsg));
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) continue;
                    options.UnhandledErrorHandler("", ex);
                }
            }
        }

        // omajinai.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T Deserialize<T>(ReadOnlyMemory<byte> buffer, MessagePackSerializerOptions options)
        {
            if (buffer.IsEmpty && MemoryMarshal.TryGetArray(buffer, out var segment))
            {
                buffer = segment;
            }
            return MessagePackSerializer.Deserialize<T>(buffer, options);
        }

        static async ValueTask ReadFullyAsync(byte[] buffer, SocketTcpClient client, int index, int remain, CancellationToken token)
        {
            while (remain > 0)
            {
                var len = await client.ReceiveAsync(buffer, index, remain, token).ConfigureAwait(false);
                index += len;
                remain -= len;
            }
        }

        public void Dispose()
        {
            channel.Writer.TryComplete();

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();

            if (server.IsValueCreated)
            {
                server.Value.Dispose();
            }

            if (client.IsValueCreated)
            {
                client.Value.Dispose();
            }
        }
    }
}

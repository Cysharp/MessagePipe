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
using Cysharp.Threading.Tasks;

namespace MessagePipe.Interprocess.Workers
{
    [Preserve]
    public sealed class TcpWorker : IDisposable
    {
        readonly IServiceProvider provider;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly IAsyncPublisher<IInterprocessKey, IInterprocessValue> publisher;
        readonly MessagePipeInterprocessTcpOptions options;

        // Channel is used from publisher for thread safety of write packet
        int initializedServer = 0;
        Lazy<SocketTcpServer> server;
        Channel<byte[]> channel;

        int initializedClient = 0;
        Lazy<SocketTcpClient> client;

        // request-response
        int messageId = 0;
        ConcurrentDictionary<int, UniTaskCompletionSource<IInterprocessValue>> responseCompletions = new ConcurrentDictionary<int, UniTaskCompletionSource<IInterprocessValue>>();

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

            if (options.AsServer != null && options.AsServer.Value)
            {
                StartReceiver();
            }
        }

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

        public async UniTask<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref initializedClient) == 1) // first incr, channel not yet started
            {
                _ = client.Value; // init
                RunPublishLoop();
            }

            var mid = Interlocked.Increment(ref messageId);
            var tcs = new UniTaskCompletionSource<IInterprocessValue>();
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
                        if (ex is OperationCanceledException) continue;

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
            while (!token.IsCancellationRequested)
            {
                ReadOnlyMemory<byte> value = Array.Empty<byte>();
                try
                {
                    var readLen = await client.ReceiveAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    if (readLen == 0) return; // end of stream(disconnect)

                    var messageLen = MessageBuilder.FetchMessageLength(buffer);
                    if (readLen == (messageLen + 4))
                    {
                        value = buffer.AsMemory(4, messageLen); // skip length header
                    }
                    else
                    {
                        // read more
                        if (buffer.Length < (messageLen + 4))
                        {
                            Array.Resize(ref buffer, messageLen + 4);
                        }
                        var remain = messageLen - (readLen - 4);
                        await ReadFullyAsync(buffer, client, readLen, remain, token).ConfigureAwait(false);
                        value = buffer.AsMemory(4, messageLen);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) continue;

                    // network error, terminate.
                    options.UnhandledErrorHandler("network error, receive loop will terminate." + Environment.NewLine, ex);
                    return;
                }

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
                                var (mid, (reqTypeName, resTypeName)) = MessagePackSerializer.Deserialize<(int, (string, string))>(message.KeyMemory, options.MessagePackSerializerOptions);
                                byte[] resultBytes;
                                try
                                {
                                    var t = AsyncRequestHandlerRegistory.Get(reqTypeName, resTypeName);
                                    var interfaceType = t.GetInterfaces().First(x => x.IsGenericType && x.Name.StartsWith("IAsyncRequestHandler"));
                                    var coreInterfaceType = t.GetInterfaces().First(x => x.IsGenericType && x.Name.StartsWith("IAsyncRequestHandlerCore"));
                                    var service = provider.GetRequiredService(interfaceType); // IAsyncRequestHandler<TRequest,TResponse>
                                    var genericArgs = interfaceType.GetGenericArguments(); // [TRequest, TResponse]
                                    var request = MessagePackSerializer.Deserialize(genericArgs[0], message.ValueMemory, options.MessagePackSerializerOptions);
                                    var responseTask = coreInterfaceType.GetMethod("InvokeAsync").Invoke(service, new[] { request, CancellationToken.None });
                                    var task = typeof(UniTask<>).MakeGenericType(genericArgs[1]).GetMethod("AsTask").Invoke(responseTask, null);
                                    await ((System.Threading.Tasks.Task)task); // Task<T> -> Task
                                    var result = task.GetType().GetProperty("Result").GetValue(task);
                                    resultBytes = MessageBuilder.BuildRemoteResponseMessage(mid, genericArgs[1], result, options.MessagePackSerializerOptions);
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
                                var mid = MessagePackSerializer.Deserialize<int>(message.KeyMemory, options.MessagePackSerializerOptions);
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

        static async UniTask ReadFullyAsync(byte[] buffer, SocketTcpClient client, int index, int remain, CancellationToken token)
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

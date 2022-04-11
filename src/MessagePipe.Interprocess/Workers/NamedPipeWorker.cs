using MessagePack;
using MessagePipe.Interprocess.Internal;
#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
#endif
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MessagePipe.Interprocess.Workers
{
    [Preserve]
    public sealed class NamedPipeWorker : IDisposable
    {
        readonly string pipeName;
        readonly IServiceProvider provider;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly IAsyncPublisher<IInterprocessKey, IInterprocessValue> publisher;
        readonly MessagePipeInterprocessNamedPipeOptions options;

        // Channel is used from publisher for thread safety of write packet
        int initializedServer = 0;
        Lazy<NamedPipeServerStream> server;
        Channel<byte[]> channel;

        int initializedClient = 0;
        Lazy<NamedPipeClientStream> client;

        // request-response
        int messageId = 0;
        ConcurrentDictionary<int, TaskCompletionSource<IInterprocessValue>> responseCompletions = new ConcurrentDictionary<int, TaskCompletionSource<IInterprocessValue>>();

        // create from DI
        [Preserve]
        public NamedPipeWorker(IServiceProvider provider, MessagePipeInterprocessNamedPipeOptions options, IAsyncPublisher<IInterprocessKey, IInterprocessValue> publisher)
        {
            this.pipeName = options.PipeName;
            this.provider = provider;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.options = options;
            this.publisher = publisher;

            this.server = CreateLazyServerStream();

            this.client = new Lazy<NamedPipeClientStream>(() =>
            {
                return new NamedPipeClientStream(options.ServerName, options.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
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

        Lazy<NamedPipeServerStream> CreateLazyServerStream()
        {
            return new Lazy<NamedPipeServerStream>(() => new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous));
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

        public void StartReceiver()
        {
            if (Interlocked.Increment(ref initializedServer) == 1) // first incr, channel not yet started
            {
                RunReceiveLoop(server.Value, x =>
                {
#if !UNITY_2018_3_OR_NEWER
                    return server.Value.WaitForConnectionAsync(x);
#else
                    return System.Threading.Tasks.Task.Run(()=> server.Value.WaitForConnection(), x);
#endif
                });
            }
        }

        // Send packet to udp socket from publisher
        async void RunPublishLoop()
        {
            var reader = channel.Reader;
            var token = cancellationTokenSource.Token;
            var pipeStream = client.Value;

            try
            {
#if !UNITY_2018_3_OR_NEWER
                await pipeStream.ConnectAsync(Timeout.Infinite, token).ConfigureAwait(false);
#else
                await System.Threading.Tasks.Task.Run(() => pipeStream.Connect(), token);
#endif
            }
            catch (IOException)
            {
                return; // connection closed.
            }
            RunReceiveLoop(pipeStream, null); // client connected, setup receive loop

            while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    try
                    {
                        await pipeStream.WriteAsync(item, 0, item.Length, token).ConfigureAwait(false);
                    }
                    catch (IOException)
                    {
                        return; // connection closed.
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

        // Receive from udp socket and push value to subscribers.
        async void RunReceiveLoop(Stream pipeStream, Func<CancellationToken, Task>? waitForConnection)
        {
        RECONNECT:
            var token = cancellationTokenSource.Token;
            if (waitForConnection != null)
            {
                try
                {
                    await waitForConnection(token).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    return; // connection closed.
                }
            }
            var buffer = new byte[65536];
            while (!token.IsCancellationRequested)
            {
                ReadOnlyMemory<byte> value = Array.Empty<byte>();
                try
                {
                    var readLen = await pipeStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    if (readLen == 0)
                    {
                        if (waitForConnection != null)
                        {
                            server.Value.Dispose();
                            server = CreateLazyServerStream();
                            pipeStream = server.Value;
                            goto RECONNECT; // end of stream(disconnect, wait reconnect)
                        }
                    }

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
                        await ReadFullyAsync(buffer, pipeStream, readLen, remain, token).ConfigureAwait(false);
                        value = buffer.AsMemory(4, messageLen);
                    }
                }
                catch (IOException)
                {
                    return; // connection closed.
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) return;
                    if (token.IsCancellationRequested) return;

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
                                    var request = MessagePackSerializer.Deserialize(genericArgs[0], message.ValueMemory, options.MessagePackSerializerOptions);
                                    var responseTask = coreInterfaceType.GetMethod("InvokeAsync")!.Invoke(service, new[] { request, CancellationToken.None });
                                    var task = typeof(ValueTask<>).MakeGenericType(genericArgs[1]).GetMethod("AsTask")!.Invoke(responseTask, null);
                                    await ((System.Threading.Tasks.Task)task!); // Task<T> -> Task
                                    var result = task.GetType().GetProperty("Result")!.GetValue(task);
                                    resultBytes = MessageBuilder.BuildRemoteResponseMessage(mid, genericArgs[1], result!, options.MessagePackSerializerOptions);
                                }
                                catch (Exception ex)
                                {
                                    // NOTE: ok to send stacktrace?
                                    resultBytes = MessageBuilder.BuildRemoteResponseError(mid, ex.ToString(), options.MessagePackSerializerOptions);
                                }

                                await pipeStream.WriteAsync(resultBytes, 0, resultBytes.Length).ConfigureAwait(false);
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
                catch (IOException)
                {
                    return; // connection closed.
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

        static async ValueTask ReadFullyAsync(byte[] buffer, Stream stream, int index, int remain, CancellationToken token)
        {
            while (remain > 0)
            {
                var len = await stream.ReadAsync(buffer, index, remain, token).ConfigureAwait(false);
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

            foreach (var item in responseCompletions)
            {
                try
                {
                    item.Value.TrySetCanceled();
                }
                catch { }
            }
        }
    }
}

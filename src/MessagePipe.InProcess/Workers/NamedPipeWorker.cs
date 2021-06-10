using MessagePipe.InProcess.Internal;
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Channels;

namespace MessagePipe.InProcess.Workers
{
    [Preserve]
    public sealed class NamedPipeWorker : IDisposable
    {
        readonly CancellationTokenSource cancellationTokenSource;
        readonly IAsyncPublisher<IInProcessKey, IInProcessValue> publisher;
        readonly MessagePipeInProcessNamedPipeOptions options;

        // Channel is used from publisher for thread safety of write packet
        int initializedServer = 0;
        Lazy<NamedPipeServerStream> server;
        Channel<byte[]> channel;

        int initializedReceiver = 0;
        Lazy<NamedPipeClientStream> client;

        // create from DI
        [Preserve]
        public NamedPipeWorker(MessagePipeInProcessNamedPipeOptions options, IAsyncPublisher<IInProcessKey, IInProcessValue> publisher)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.options = options;
            this.publisher = publisher;

            this.server = new Lazy<NamedPipeServerStream>(() =>
            {
                return new NamedPipeServerStream(options.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            });

            this.client = new Lazy<NamedPipeClientStream>(() =>
            {
                // TODO: serverName
                return new NamedPipeClientStream(".", options.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            });

            this.channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions()
            {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = false
            });
        }

        public void Publish<TKey, TMessage>(TKey key, TMessage message)
        {
            if (Interlocked.Increment(ref initializedServer) == 1) // first incr, channel not yet started
            {
                _ = client.Value; // init
                RunPublishLoop();
            }

            var bufferWriter = new ArrayPoolBufferWriter();
            MessageBuilder.WriteMessage(bufferWriter, key, message, options.MessagePackSerializerOptions);
            var buffer = bufferWriter.WrittenSpan.ToArray();
            channel.Writer.TryWrite(buffer);
        }

        // Send packet to udp socket from publisher
        async void RunPublishLoop()
        {
            var reader = channel.Reader;
            var token = cancellationTokenSource.Token;
            var pipeStream = client.Value;

            await pipeStream.ConnectAsync(10000, token).ConfigureAwait(false); // TODO:Timeout

            while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    try
                    {
                        await pipeStream.WriteAsync(item, 0, item.Length, token).ConfigureAwait(false);
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
            if (Interlocked.Increment(ref initializedReceiver) == 1) // first incr, channel not yet started
            {
                _ = server.Value; // init
                RunReceiveLoop();
            }
        }

        // Receive from udp socket and push value to subscribers.
        async void RunReceiveLoop()
        {
            var token = cancellationTokenSource.Token;
            var pipeStream = server.Value;
            await pipeStream.WaitForConnectionAsync(token).ConfigureAwait(false);
            var buffer = new byte[65536];
            while (!token.IsCancellationRequested)
            {
                ReadOnlyMemory<byte> value;
                try
                {
                    // TODO:Read Fully
                    var readLen = await pipeStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    value = buffer.AsMemory(0, readLen);
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
                    var message = MessageBuilder.ReadMessage(value.ToArray(), options.MessagePackSerializerOptions);
                    publisher.Publish(message, message, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) continue;
                    options.UnhandledErrorHandler("", ex);
                }
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

using MessagePipe.InProcess.Internal;
using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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

            var buffer = MessageBuilder.BuildPubSubMessage(key, message, options.MessagePackSerializerOptions);
            channel.Writer.TryWrite(buffer);
        }

        // Send packet to udp socket from publisher
        async void RunPublishLoop()
        {
            var reader = channel.Reader;
            var token = cancellationTokenSource.Token;
            var pipeStream = client.Value;

            await pipeStream.ConnectAsync(Timeout.Infinite, token).ConfigureAwait(false);

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
        RECONNECT:
            var token = cancellationTokenSource.Token;
            var pipeStream = server.Value;
            await pipeStream.WaitForConnectionAsync(token).ConfigureAwait(false);
            var buffer = new byte[65536];
            while (!token.IsCancellationRequested)
            {
                ReadOnlyMemory<byte> value = Array.Empty<byte>();
                try
                {
                    var readLen = await pipeStream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    if (readLen == 0) goto RECONNECT; // end of stream(disconnect, wait reconnect)

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
                    publisher.Publish(message, message, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) continue;
                    options.UnhandledErrorHandler("", ex);
                }
            }
        }

        static async Task ReadFullyAsync(byte[] buffer, Stream stream, int index, int remain, CancellationToken token)
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
        }
    }
}

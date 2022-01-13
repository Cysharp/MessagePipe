using MessagePipe.Interprocess.Internal;
using System;
using System.Threading;
#if !UNITY_2018_3_OR_NEWER
using System.Threading.Channels;
#else
using Cysharp.Threading.Tasks;
#endif

namespace MessagePipe.Interprocess.Workers
{
    [Preserve]
    public sealed class UdpWorker : IDisposable
    {
        readonly CancellationTokenSource cancellationTokenSource;
        readonly IAsyncPublisher<IInterprocessKey, IInterprocessValue> publisher;
        readonly MessagePipeInterprocessOptions options;

        // Channel is used from publisher for thread safety of write packet
        int initializedServer = 0;
        Lazy<SocketUdpServer> server;
        Channel<byte[]> channel;

        int initializedClient = 0;
        Lazy<SocketUdpClient> client;

        // create from DI
        [Preserve]
        public UdpWorker(MessagePipeInterprocessUdpOptions options, IAsyncPublisher<IInterprocessKey, IInterprocessValue> publisher)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.options = options;
            this.publisher = publisher;

            this.server = new Lazy<SocketUdpServer>(() =>
            {
                return SocketUdpServer.Bind(options.Port, 0x10000);
            });

            this.client = new Lazy<SocketUdpClient>(() =>
            {
                return SocketUdpClient.Connect(options.Host, options.Port, 0x10000);
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
        }
#if NET5_0_OR_GREATER
        [Preserve]
        public UdpWorker(MessagePipeInterprocessUdpUdsOptions options, IAsyncPublisher<IInterprocessKey, IInterprocessValue> publisher)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.options = options;
            this.publisher = publisher;

            this.server = new Lazy<SocketUdpServer>(() =>
            {
                return SocketUdpServer.BindUds(options.SocketPath, 0x10000);
            });

            this.client = new Lazy<SocketUdpClient>(() =>
            {
                return SocketUdpClient.ConnectUds(options.SocketPath, 0x10000);
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

        // Send packet to udp socket from publisher
        async void RunPublishLoop()
        {
            var reader = channel.Reader;
            var token = cancellationTokenSource.Token;
            var udpClient = client.Value;
            while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    try
                    {
                        await udpClient.SendAsync(item, token).ConfigureAwait(false);
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
                _ = server.Value; // init
                RunReceiveLoop();
            }
        }

        // Receive from udp socket and push value to subscribers.
        async void RunReceiveLoop()
        {
            var token = cancellationTokenSource.Token;
            var udpServer = server.Value;
            while (!token.IsCancellationRequested)
            {
                ReadOnlyMemory<byte> value;
                try
                {
                    value = await udpServer.ReceiveAsync(token).ConfigureAwait(false);
                    if (value.Length == 0) return; // invalid data?
                    var len = MessageBuilder.FetchMessageLength(value.Span);
                    if (len != value.Length - 4)
                    {
                        throw new InvalidOperationException("Receive invalid message size.");
                    }
                    value = value.Slice(4);
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
                    var message = MessageBuilder.ReadPubSubMessage(value.ToArray());
                    publisher.Publish(message, message, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException) return;
                    if (token.IsCancellationRequested) return;
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

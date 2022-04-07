using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace MessagePipe.Interprocess.Benchmark
{
    [ShortRunJob]
    public class TcpBenchmark
    {
        IServiceProvider? _TcpIpProvider = null;
        IServiceProvider? _TcpUdsProvider = null;
        string? _TcpUdsSocketPath = null;
        [Params(1, 1000000)]
        public int DataSize { get; set; }
        IServiceProvider CreateTcpIpServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddMessagePipe();
            services.AddMessagePipeTcpInterprocess("127.0.0.1", 10000, opts =>
            {
                opts.HostAsServer = true;
            });
            return services.BuildServiceProvider();
        }
        IServiceProvider CreateTcpUdsServiceProvider()
        {
            _TcpUdsSocketPath = System.IO.Path.GetTempFileName();
            if (System.IO.File.Exists(_TcpUdsSocketPath))
            {
                System.IO.File.Delete(_TcpUdsSocketPath);
            }
            var services = new ServiceCollection();
            services.AddMessagePipe();
            //services.AddMessagePipeTcpInterprocessUds(_TcpUdsSocketPath, opt =>
            //{
            //    opt.HostAsServer = true;
            //    opt.SendBufferSize = Math.Min(DataSize, 0x1000);
            //    opt.ReceiveBufferSize = Math.Min(DataSize, 0x1000);
            //});
            return services.BuildServiceProvider();
        }
        [GlobalSetup]
        public void Setup()
        {
            _TcpIpProvider = CreateTcpIpServiceProvider();
            _TcpUdsProvider = CreateTcpUdsServiceProvider();
        }
        [GlobalCleanup]
        public void Cleanup()
        {
            if (_TcpUdsSocketPath != null)
            {
                if (System.IO.File.Exists(_TcpUdsSocketPath))
                {
                    System.IO.File.Delete(_TcpUdsSocketPath);
                }
            }
        }
        async ValueTask RemoteRequest(IRemoteRequestHandler<int, byte[]>? handler)
        {
            if(handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            await handler.InvokeAsync(DataSize);
        }
        async ValueTask PubSub(IServiceProvider provider)
        {
            var publisher = provider.GetService<IDistributedPublisher<int, byte[]>>() ?? throw new ArgumentNullException("publisher");
            var subscriber = provider.GetService<IDistributedSubscriber<int, byte[]>>() ?? throw new ArgumentNullException("subscriber");
            await using (await subscriber.SubscribeAsync(1, new MyAsyncMessageHandler()))
            {
                await publisher.PublishAsync(1, new byte[DataSize]);
            }
        }
        [Benchmark]
        public async ValueTask TcpIpRemoteRequest()
        {
            var provider = _TcpIpProvider ?? throw new ArgumentNullException("_TcpIpProvider");
            var handler = provider.GetService<IRemoteRequestHandler<int, byte[]>>();
            await RemoteRequest(handler);
        }
        [Benchmark]
        public async ValueTask TcpUdsRemoteRequest()
        {
            var provider = _TcpUdsProvider ?? throw new ArgumentNullException("_TcpUdsProvider");
            var handler = provider.GetService<IRemoteRequestHandler<int, byte[]>>();
            await RemoteRequest(handler);
        }
        [Benchmark]
        public async ValueTask TcpIpPubSub()
        {
            var provider = _TcpIpProvider ?? throw new ArgumentNullException("_TcpUdsProvider");
            await PubSub(provider);
        }
        [Benchmark]
        public async ValueTask TcpUdsPubSub()
        {
            var provider = _TcpUdsProvider ?? throw new ArgumentNullException("_TcpUdsProvider");
            await PubSub(provider);
        }
    }
}

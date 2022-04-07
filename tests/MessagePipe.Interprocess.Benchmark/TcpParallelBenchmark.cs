using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace MessagePipe.Interprocess.Benchmark
{
    [ShortRunJob]
    public class TcpParallelBenchmark
    {
        [Params(100)]
        public int DataSize { get; set; }
        [Params(1, 10)]
        public int TaskNum { get; set; }
        const int TotalRequestNum = 10;
        IServiceProvider CreateTcpIpServiceProvider(string ip, int port)
        {
            var services = new ServiceCollection();
            services.AddMessagePipe();
            services.AddMessagePipeTcpInterprocess(ip, port, opts =>
            {
                opts.HostAsServer = true;
            });
            return services.BuildServiceProvider();
        }
        IServiceProvider CreateTcpUdsServiceProvider(string socketPath)
        {
            if (System.IO.File.Exists(socketPath))
            {
                System.IO.File.Delete(socketPath);
            }
            var services = new ServiceCollection();
            services.AddMessagePipe();
            //services.AddMessagePipeTcpInterprocessUds(socketPath, opt =>
            //{
            //    opt.HostAsServer = true;
            //    opt.SendBufferSize = Math.Min(DataSize, 0x1000);
            //    opt.ReceiveBufferSize = Math.Min(DataSize, 0x1000);
            //});
            return services.BuildServiceProvider();
        }
        IServiceProvider? _TcpIpProvider = null;
        IServiceProvider? _TcpUdsProvider = null;
        string _TcpSocketPath = "";
        [GlobalSetup]
        public void Setup()
        {
            _TcpIpProvider = CreateTcpIpServiceProvider("127.0.0.1", 20000);
            _TcpSocketPath = Path.GetTempFileName();
            _TcpUdsProvider = CreateTcpUdsServiceProvider(_TcpSocketPath);
        }
        [GlobalCleanup]
        public void Cleanup()
        {
            if (File.Exists(_TcpSocketPath))
            {
                File.Delete(_TcpSocketPath);
            }
        }
        async Task ClientTask(IServiceProvider provider, int num)
        {
            await Task.WhenAll(Enumerable.Range(0, num).Select(async idx =>
            {
                var handler = provider.GetService<IRemoteRequestHandler<int, byte[]>>() ?? throw new ArgumentNullException("handler");
                for (int i = 0; i < TotalRequestNum / num; i++)
                {
                    await handler.InvokeAsync(DataSize);
                }
            }));
        }
        async Task SubscriberTask(IServiceProvider provider, CancellationToken token)
        {
            var subscriber = provider.GetService<IDistributedSubscriber<int, byte[]>>() ?? throw new ArgumentNullException("subscriber");
            await using (await subscriber.SubscribeAsync(1, new MyAsyncMessageHandler(), token))
            {

            }
        }
        async Task PublisherAsync(IServiceProvider provider, int taskNum, CancellationTokenSource cts)
        {
            await Task.WhenAll(Enumerable.Range(0, taskNum).Select(async _ =>
            {
                var publisher = provider.GetService<IDistributedPublisher<int, byte[]>>() ?? throw new ArgumentNullException("provider");
                await publisher.PublishAsync(1, new byte[DataSize]);
            }));
            cts.Cancel();
        }
        [Benchmark]
        public async Task TcpIpRemoteRequest()
        {
            if(_TcpIpProvider == null)
            {
                throw new ArgumentNullException(nameof(_TcpIpProvider));
            }
            await ClientTask(_TcpIpProvider, TaskNum);
        }
        [Benchmark]
        public async Task TcpUdsRemoteRequest()
        {
            if(_TcpUdsProvider == null)
            {
                throw new ArgumentNullException(nameof(_TcpUdsProvider));
            }
            await ClientTask(_TcpUdsProvider, TaskNum);
        }
        [Benchmark]
        public async Task TcpIpPubSub()
        {
            if (_TcpIpProvider == null)
            {
                throw new ArgumentNullException(nameof(_TcpIpProvider));
            }
            using var cts = new CancellationTokenSource();
            await Task.WhenAll(PublisherAsync(_TcpIpProvider, TaskNum, cts), SubscriberTask(_TcpIpProvider, cts.Token));
        }
        [Benchmark]
        public async Task TcpUdsPubSub()
        {
            if(_TcpUdsProvider == null)
            {
                throw new ArgumentNullException(nameof(_TcpUdsProvider));
            }
            using var cts = new CancellationTokenSource();
            await Task.WhenAll(PublisherAsync(_TcpUdsProvider, TaskNum, cts), SubscriberTask(_TcpUdsProvider, cts.Token));
        }
    }
}

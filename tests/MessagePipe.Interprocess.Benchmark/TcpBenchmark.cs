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
            services.AddMessagePipeTcpInterprocessUds(_TcpUdsSocketPath, opt =>
            {
                opt.HostAsServer = true;
            });
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
            await handler.InvokeAsync(1);
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
    }
}

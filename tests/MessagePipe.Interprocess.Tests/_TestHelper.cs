using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit.Abstractions;

namespace MessagePipe.Interprocess.Tests
{
    public static class TestHelper
    {
        public static IServiceProvider BuildServiceProviderUdp(string host, int port, ITestOutputHelper helper)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeUdpInterprocess(host, port, x =>
            {
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }
        public static IServiceProvider BuildServiceProviderUdpWithUds(string domainSocketPath, ITestOutputHelper helper)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeUdpInterprocessUds(domainSocketPath, x =>
            {
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }
        public static IServiceProvider BuildServiceProviderTcp(string host, int port, ITestOutputHelper helper, bool asServer = true)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeTcpInterprocess(host, port, x =>
            {
                x.HostAsServer = asServer;
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }
        public static IServiceProvider BuildServiceProviderTcpWithUds(string domainSocketPath, ITestOutputHelper helper, bool asServer = true)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeTcpInterprocessUds(domainSocketPath, x =>
            {
                x.HostAsServer = asServer;
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider BuildServiceProviderNamedPipe(string pipeName, ITestOutputHelper helper, bool asServer = true)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeNamedPipeInterprocess(pipeName, x =>
            {
                x.HostAsServer = asServer;
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }
    }
}
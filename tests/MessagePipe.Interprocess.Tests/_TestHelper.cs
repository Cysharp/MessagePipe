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
            sc.AddMessagePipeInterprocessUdp(host, port, x =>
            {
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }
        public static IServiceProvider BuildServiceProviderTcp(string host, int port, ITestOutputHelper helper, bool asServer = true)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeInterprocessTcp(host, port, x =>
            {
                x.AsServer = asServer;
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider BuildServiceProviderNamedPipe(string pipeName, ITestOutputHelper helper, bool asServer = true)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeInterprocessNamedPipe(pipeName, x =>
            {
                x.AsServer = asServer;
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }
    }
}
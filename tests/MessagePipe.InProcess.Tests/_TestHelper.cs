using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit.Abstractions;

namespace MessagePipe.InProcess.Tests
{
    public static class TestHelper
    {
        public static IServiceProvider BuildServiceProviderUdp(string host, int port, ITestOutputHelper helper)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeInProcessUdp(host, port, x =>
            {
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }
        public static IServiceProvider BuildServiceProviderTcp(string host, int port, ITestOutputHelper helper)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeInProcessTcp(host, port, x =>
            {
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider BuildServiceProviderNamedPipe(string pipeName, ITestOutputHelper helper)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeInProcessNamedPipe(pipeName, x =>
            {
                x.UnhandledErrorHandler = (msg, e) => helper.WriteLine(msg + e);
            });
            return sc.BuildServiceProvider();
        }
    }
}
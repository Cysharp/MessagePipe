using Microsoft.Extensions.DependencyInjection;
using System;

namespace MessagePipe.Tests
{
    public static class TestHelper
    {
        public static IServiceProvider BuildServiceProvider()
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider BuildServiceProvider(Action<MessagePipeOptions> configure)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe(configure);
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider BuildServiceProvider2(Action<ServiceCollection> configureService)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            configureService(sc);
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider BuildServiceProvider3(Action<MessagePipeOptions> configure, Action<ServiceCollection> configureService)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe(configure);
            configureService(sc);
            return sc.BuildServiceProvider();
        }
    }
}
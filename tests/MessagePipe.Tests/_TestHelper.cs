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

        public static IServiceProvider BuildServiceProvider2(Action<IMessagePipeBuilder> configureService)
        {
            var sc = new ServiceCollection();
            var builder = sc.AddMessagePipe();
            configureService(builder);
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider BuildServiceProvider3(Action<MessagePipeOptions> configure, Action<IMessagePipeBuilder> configureService)
        {
            var sc = new ServiceCollection();
            var builder = sc.AddMessagePipe(configure);
            configureService(builder);
            return sc.BuildServiceProvider();
        }
    }
}
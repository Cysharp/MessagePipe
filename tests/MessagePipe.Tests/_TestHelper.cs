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
    }
}
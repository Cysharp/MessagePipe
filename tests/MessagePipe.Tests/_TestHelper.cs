using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

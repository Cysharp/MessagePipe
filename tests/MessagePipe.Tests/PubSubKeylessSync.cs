using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xunit;

// for check diagnostics, modify namespace.
namespace __MessagePipe.Tests
{
    public class PubsubKeylessSync
    {
        [Fact]
        public void SimplePush()
        {
            var provider = TestHelper.BuildServiceProvider();

            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var p = provider.GetRequiredService<IPublisher<string>>();
            var s = provider.GetRequiredService<ISubscriber<string>>();

            var result = new List<string>();
            var d1 = s.Subscribe(x => result.Add("1:" + x));
            var d2 = s.Subscribe(x => result.Add("2:" + x));
            var d3 = s.Subscribe(x => result.Add("3:" + x));

            Assert.Equal(3, info.SubscribeCount);

            // use BeEquivalentTo, allow different order

            p.Publish("one");
            Assert.Equivalent(new[] { "1:one", "2:one", "3:one" }, result, strict: true);
            result.Clear();

            p.Publish("one");
            Assert.Equivalent(new[] { "1:one", "2:one", "3:one" }, result, strict: true);
            result.Clear();

            d2.Dispose();

            p.Publish("two");
            Assert.Equivalent(new[] { "1:two", "3:two" }, result, strict: true);
            result.Clear();

            d3.Dispose();
            p.Publish("three");
            Assert.Equivalent(new[] { "1:three" }, result, strict: true);
            result.Clear();

            d1.Dispose();
            Assert.Empty(result);
            result.Clear();

            Assert.Equal(0, info.SubscribeCount);
        }
    }
}

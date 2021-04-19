using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// for check diagnostics, modify namespace.
namespace __MessagePipe.Tests
{
    public class RedisPubsubKeylessSync
    {
        [Fact]
        public async Task SimplePush()
        {
            var conection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync("localhost");
            var provider = TestHelper.BuildRedisServiceProvider(conection);

            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var p = provider.GetRequiredService<IPublisher<string>>();
            var s = provider.GetRequiredService<ISubscriber<string>>();

            var result = new List<string>();
            var d1 = s.Subscribe(x => result.Add("1:" + x));
            var d2 = s.Subscribe(x => result.Add("2:" + x));
            var d3 = s.Subscribe(x => result.Add("3:" + x));

            info.SubscribeCount.Should().Be(3);

            // use BeEquivalentTo, allow different order

            p.Publish("one");
            result.Should().BeEquivalentTo("1:one", "2:one", "3:one");
            result.Clear();

            p.Publish("one");
            result.Should().BeEquivalentTo("1:one", "2:one", "3:one");
            result.Clear();

            d2.Dispose();

            p.Publish("two");
            result.Should().BeEquivalentTo("1:two", "3:two");
            result.Clear();

            d3.Dispose();
            p.Publish("three");
            result.Should().BeEquivalentTo("1:three");
            result.Clear();

            d1.Dispose();
            result.Should().Equal();
            result.Clear();

            info.SubscribeCount.Should().Be(0);
        }
    }
}

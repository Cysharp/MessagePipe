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
    public class RedisPubSubKeyedASync
    {
        [Fact]
        public async Task SimplePush()
        {
            const string Key1 = "foo";
            const string Key2 = "bar";

            var conection = TestHelper.GetLocalConnectionMultiplexer();
            var provider = TestHelper.BuildRedisServiceProvider(conection);

            var p = provider.GetRequiredService<IDistributedPublisher<string, string>>();
            var s = provider.GetRequiredService<IDistributedSubscriber<string, string>>();

            var result = new List<string>();
            var d1 = await s.SubscribeAsync(Key1, (x) =>
            {
                result.Add("1:" + x);
            });
            var d2 = await s.SubscribeAsync(Key2, (x) => result.Add("2:" + x));
            var d3 = await s.SubscribeAsync(Key1, (x) => result.Add("3:" + x));

            // use BeEquivalentTo, allow different order

            await p.PublishAsync(Key1, "one");
            await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...

            result.Should().BeEquivalentTo("1:one", "3:one");
            result.Clear();

            await p.PublishAsync(Key2, "one");
            await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...

            result.Should().BeEquivalentTo("2:one");
            result.Clear();

            await d3.DisposeAsync();

            await p.PublishAsync(Key1, "two");
            await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...
            result.Should().BeEquivalentTo("1:two");
            result.Clear();

            await d1.DisposeAsync();
            await d2.DisposeAsync();

            await p.PublishAsync(Key1, "zero");
            await p.PublishAsync(Key2, "zero");
            await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...

            result.Should().Equal();
            result.Clear();
        }

    }
}

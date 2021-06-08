#pragma warning disable CS1998

using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xunit;

// for check diagnostics, modify namespace.
namespace __MessagePipe.Tests
{
    public class PubsubKeyedASync
    {
        [Fact]
        public void SimplePush()
        {
            const string Key1 = "foo";
            const string Key2 = "bar";

            var provider = TestHelper.BuildServiceProvider();

            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var p = provider.GetRequiredService<IAsyncPublisher<string, string>>();
            var s = provider.GetRequiredService<IAsyncSubscriber<string, string>>();

            var result = new List<string>();
            var d1 = s.Subscribe(Key1, async (x, _) => result.Add("1:" + x));
            var d2 = s.Subscribe(Key2, async (x, _) => result.Add("2:" + x));
            var d3 = s.Subscribe(Key1, async (x, _) => result.Add("3:" + x));

            info.SubscribeCount.Should().Be(3);

            // use BeEquivalentTo, allow different order

            p.Publish(Key1, "one");
            result.Should().BeEquivalentTo("1:one", "3:one");
            result.Clear();

            p.Publish(Key2, "one");
            result.Should().BeEquivalentTo("2:one");
            result.Clear();

            d3.Dispose();

            p.Publish(Key1, "two");
            result.Should().BeEquivalentTo("1:two");
            result.Clear();

            d1.Dispose();
            d2.Dispose();

            p.Publish(Key1, "zero");
            p.Publish(Key2, "zero");

            result.Should().Equal();
            result.Clear();

            info.SubscribeCount.Should().Be(0);
        }
    }
}

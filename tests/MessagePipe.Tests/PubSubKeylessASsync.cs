#pragma warning disable CS1998

using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// for check diagnostics, modify namespace.
namespace __MessagePipe.Tests
{
    public class PubsubKeylessAsync
    {
        [Fact]
        public async Task SameAsSync()
        {
            var provider = TestHelper.BuildServiceProvider();

            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var p = provider.GetRequiredService<IAsyncPublisher<string>>();
            var s = provider.GetRequiredService<IAsyncSubscriber<string>>();

            var result = new List<string>();
            var d1 = s.Subscribe(async (x, c) => result.Add("1:" + x));
            var d2 = s.Subscribe(async (x, c) => result.Add("2:" + x));
            var d3 = s.Subscribe(async (x, c) => result.Add("3:" + x));

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

        [Fact]
        public async Task Async()
        {
            var provider = TestHelper.BuildServiceProvider();

            var p = provider.GetRequiredService<IAsyncPublisher<string>>();
            var s = provider.GetRequiredService<IAsyncSubscriber<string>>();

            s.Subscribe(async (x, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
                Console.WriteLine("OK?");
            });

            s.Subscribe(async (x, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                Console.WriteLine("OK2?");
            });

            // require to calc time for testing...
            var cts = new CancellationTokenSource();
            await p.PublishAsync("takoyaki", AsyncPublishStrategy.Sequential, cts.Token);


        }
    }
}

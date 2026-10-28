#pragma warning disable CS1998

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
            var d1 = s.Subscribe(async (x, _) => result.Add("1:" + x));
            var d2 = s.Subscribe(async (x, _) => result.Add("2:" + x));
            var d3 = s.Subscribe(async (x, _) => result.Add("3:" + x));

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

        [Fact]
        public async Task Async()
        {
            var provider = TestHelper.BuildServiceProvider();

            var p = provider.GetRequiredService<IAsyncPublisher<string>>();
            var s = provider.GetRequiredService<IAsyncSubscriber<string>>();

            s.Subscribe(async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
                Console.WriteLine("OK?");
            });

            s.Subscribe(async (_, ct) =>
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

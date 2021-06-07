using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MessagePipe.Tests
{
    public class AsAsyncEnumerableTest
    {
        [Fact]
        public async Task Standard()
        {
            var p = TestHelper.BuildServiceProvider();
            var publisher = p.GetRequiredService<IAsyncPublisher<int>>();
            var subscriber = p.GetRequiredService<IAsyncSubscriber<int>>();
            var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();


            var l = new List<int>();
            var cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                for (int i = 0; i < 3; i++)
                {
                    publisher.Publish(i);
                    await Task.Yield();
                }
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                cts.Cancel();
            });

            try
            {
                await foreach (var item in subscriber.AsAsyncEnumerable().WithCancellation(cts.Token))
                {
                    l.Add(item);
                }
            }
            catch (OperationCanceledException) { }

            l.Should().Equal(0, 1, 2);

            monitor.SubscribeCount.Should().Be(0);
        }

        [Fact]
        public async Task Keyed()
        {
            var p = TestHelper.BuildServiceProvider();
            var publisher = p.GetRequiredService<IAsyncPublisher<string, int>>();
            var subscriber = p.GetRequiredService<IAsyncSubscriber<string, int>>();
            var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();


            var l = new List<int>();
            var cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                for (int i = 0; i < 3; i++)
                {
                    publisher.Publish("foo", i);
                    await Task.Yield();
                }
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                cts.Cancel();
            });

            try
            {
                await foreach (var item in subscriber.AsAsyncEnumerable("foo").WithCancellation(cts.Token))
                {
                    l.Add(item);
                }
            }
            catch (OperationCanceledException) { }

            l.Should().Equal(0, 1, 2);

            monitor.SubscribeCount.Should().Be(0);
        }

        [Fact]
        public async Task Buffered()
        {
            var p = TestHelper.BuildServiceProvider();
            var publisher = p.GetRequiredService<IBufferedAsyncPublisher<int>>();
            var subscriber = p.GetRequiredService<IBufferedAsyncSubscriber<int>>();
            var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();

            await publisher.PublishAsync(99);

            var l = new List<int>();
            var cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                for (int i = 0; i < 3; i++)
                {
                    publisher.Publish(i);
                    await Task.Yield();
                }
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                cts.Cancel();
            });

            try
            {
                await foreach (var item in subscriber.AsAsyncEnumerable().WithCancellation(cts.Token))
                {
                    l.Add(item);
                }
            }
            catch (OperationCanceledException) { }

            l.Should().Equal(99, 0, 1, 2);

            monitor.SubscribeCount.Should().Be(0);
        }
    }
}

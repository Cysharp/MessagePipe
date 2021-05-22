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
    public class FirstAsyncTest
    {
        [Fact]
        public async Task NonCancellationStandard()
        {
            var p = TestHelper.BuildServiceProvider();
            var publisher = p.GetRequiredService<IPublisher<int>>();
            var subscriber = p.GetRequiredService<ISubscriber<int>>();
            var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();

            var task = subscriber.FirstAsync(CancellationToken.None);

            monitor.SubscribeCount.Should().Be(1);

            publisher.Publish(100);

            var r = await task;

            r.Should().Be(100);

            monitor.SubscribeCount.Should().Be(0);
        }

        [Fact]
        public async Task CancellationStandard()
        {
            var p = TestHelper.BuildServiceProvider();
            var publisher = p.GetRequiredService<IPublisher<int>>();
            var subscriber = p.GetRequiredService<ISubscriber<int>>();
            var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();

            var cts = new CancellationTokenSource();

            var task = subscriber.FirstAsync(cts.Token);

            monitor.SubscribeCount.Should().Be(1);

            cts.Cancel();

            (await Assert.ThrowsAsync<OperationCanceledException>(async () => await task)).CancellationToken.Should().Be(cts.Token);

            monitor.SubscribeCount.Should().Be(0);
        }

        [Fact]
        public async Task CancellationBeforeSubscribe()
        {
            var p = TestHelper.BuildServiceProvider();
            var publisher = p.GetRequiredService<IPublisher<int>>();
            var subscriber = p.GetRequiredService<ISubscriber<int>>();
            var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();

            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = subscriber.FirstAsync(cts.Token);

            monitor.SubscribeCount.Should().Be(0);

            cts.Cancel();

            (await Assert.ThrowsAsync<OperationCanceledException>(async () => await task)).CancellationToken.Should().Be(cts.Token);

            monitor.SubscribeCount.Should().Be(0);
        }

        [Fact]
        public async Task BufferedNonCancellationStandard()
        {
            {
                var p = TestHelper.BuildServiceProvider();
                var publisher = p.GetRequiredService<IBufferedPublisher<IntValue>>();
                var subscriber = p.GetRequiredService<IBufferedSubscriber<IntValue>>();
                var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();

                var task = subscriber.FirstAsync(CancellationToken.None);

                monitor.SubscribeCount.Should().Be(1);

                publisher.Publish(new IntValue(100));

                var r = await task;

                r.Value.Should().Be(100);

                monitor.SubscribeCount.Should().Be(0);
            }
            {
                var p = TestHelper.BuildServiceProvider();
                var publisher = p.GetRequiredService<IBufferedPublisher<IntValue>>();
                var subscriber = p.GetRequiredService<IBufferedSubscriber<IntValue>>();
                var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();

                publisher.Publish(new IntValue(9999));

                var task = subscriber.FirstAsync(CancellationToken.None);

                monitor.SubscribeCount.Should().Be(0);

                publisher.Publish(new IntValue(100));

                var r = await task;

                r.Value.Should().Be(9999);

                monitor.SubscribeCount.Should().Be(0);
            }
        }

        [Fact]
        public async Task NonCancellationKeyedStandard()
        {
            var p = TestHelper.BuildServiceProvider();
            var publisher = p.GetRequiredService<IPublisher<int, int>>();
            var subscriber = p.GetRequiredService<ISubscriber<int, int>>();
            var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();

            var task = subscriber.FirstAsync(9, CancellationToken.None);

            monitor.SubscribeCount.Should().Be(1);

            publisher.Publish(4, 55);
            monitor.SubscribeCount.Should().Be(1);
            publisher.Publish(9, 100);

            var r = await task;

            r.Should().Be(100);

            monitor.SubscribeCount.Should().Be(0);
        }

        [Fact]
        public async Task NonCancellationAsyncStandard()
        {
            var p = TestHelper.BuildServiceProvider();
            var publisher = p.GetRequiredService<IAsyncPublisher<int>>();
            var subscriber = p.GetRequiredService<IAsyncSubscriber<int>>();
            var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var task = subscriber.FirstAsync(cts.Token);

            monitor.SubscribeCount.Should().Be(1);

            await publisher.PublishAsync(100);

            var r = await task;

            r.Should().Be(100);

            monitor.SubscribeCount.Should().Be(0);
        }

        class IntValue
        {
            public int Value { get; set; }

            public IntValue(int value)
            {
                this.Value = value;
            }
        }
    }
}

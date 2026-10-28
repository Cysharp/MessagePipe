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

            Assert.Equal(1, monitor.SubscribeCount);

            publisher.Publish(100);

            var r = await task;

            Assert.Equal(100, r);

            Assert.Equal(0, monitor.SubscribeCount);
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

            Assert.Equal(1, monitor.SubscribeCount);

            cts.Cancel();

            Assert.Equal(cts.Token, (await Assert.ThrowsAsync<OperationCanceledException>(async () => await task)).CancellationToken);

            Assert.Equal(0, monitor.SubscribeCount);
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

            Assert.Equal(0, monitor.SubscribeCount);

            cts.Cancel();

            Assert.Equal(cts.Token, (await Assert.ThrowsAsync<OperationCanceledException>(async () => await task)).CancellationToken);

            Assert.Equal(0, monitor.SubscribeCount);
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

                Assert.Equal(1, monitor.SubscribeCount);

                publisher.Publish(new IntValue(100));

                var r = await task;

                Assert.Equal(100, r.Value);

                Assert.Equal(0, monitor.SubscribeCount);
            }
            {
                var p = TestHelper.BuildServiceProvider();
                var publisher = p.GetRequiredService<IBufferedPublisher<IntValue>>();
                var subscriber = p.GetRequiredService<IBufferedSubscriber<IntValue>>();
                var monitor = p.GetRequiredService<MessagePipeDiagnosticsInfo>();

                publisher.Publish(new IntValue(9999));

                var task = subscriber.FirstAsync(CancellationToken.None);

                Assert.Equal(0, monitor.SubscribeCount);

                publisher.Publish(new IntValue(100));

                var r = await task;

                Assert.Equal(9999, r.Value);

                Assert.Equal(0, monitor.SubscribeCount);
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

            Assert.Equal(1, monitor.SubscribeCount);

            publisher.Publish(4, 55);
            Assert.Equal(1, monitor.SubscribeCount);
            publisher.Publish(9, 100);

            var r = await task;

            Assert.Equal(100, r);

            Assert.Equal(0, monitor.SubscribeCount);
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

            Assert.Equal(1, monitor.SubscribeCount);

            await publisher.PublishAsync(100);

            var r = await task;

            Assert.Equal(100, r);

            Assert.Equal(0, monitor.SubscribeCount);
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

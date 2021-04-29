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
    public class EventFactoryTest
    {
        [Fact]
        public void SimplePubSub()
        {
            var provider = TestHelper.BuildServiceProvider();

            var evFactory = provider.GetRequiredService<EventFactory>();

            var (publisher, subscriber) = evFactory.CreateEvent<int>();

            var l = new List<int>();
            var d1 = subscriber.Subscribe(x => l.Add(x));
            var d2 = subscriber.Subscribe(x => l.Add(x * 10));

            publisher.Publish(10);
            publisher.Publish(20);

            d1.Dispose();

            publisher.Publish(30);

            d2.Dispose();
            
            publisher.Publish(40);

            l.Should().Equal(10, 100, 20, 200, 300);
        }

        [Fact]
        public void UnsubscribeFromPublisher()
        {
            var provider = TestHelper.BuildServiceProvider();

            var evFactory = provider.GetRequiredService<EventFactory>();

            var (publisher, subscriber) = evFactory.CreateEvent<int>();

            var l = new List<int>();
            var d1 = subscriber.Subscribe(x => l.Add(x));
            var d2 = subscriber.Subscribe(x => l.Add(x * 10));

            publisher.Publish(10);
            publisher.Publish(20);

            publisher.Dispose();

            publisher.Publish(30);
            publisher.Publish(40);

            l.Should().Equal(10, 100, 20, 200);

            d1.Dispose();
            d2.Dispose();
        }

        [Fact]
        public async Task AsyncSimplePubSub()
        {
            var provider = TestHelper.BuildServiceProvider();

            var evFactory = provider.GetRequiredService<EventFactory>();

            var (publisher, subscriber) = evFactory.CreateAsyncEvent<int>();

            var l = new List<int>();
            var d1 = subscriber.Subscribe(async (x, ct) => { await Task.Delay(500); lock (l) { l.Add(x); } });
            var d2 = subscriber.Subscribe(async (x, ct) => { await Task.Delay(300); lock (l) { l.Add(x * 10); } });

            await publisher.PublishAsync(10);
            await publisher.PublishAsync(20);

            d1.Dispose();

            await publisher.PublishAsync(30);

            d2.Dispose();

            await publisher.PublishAsync(40);

            l.Should().Equal(100, 10, 200, 20, 300);
        }

        [Fact]
        public async Task AsyncUnsubscribeFromPublisher()
        {
            var provider = TestHelper.BuildServiceProvider();

            var evFactory = provider.GetRequiredService<EventFactory>();

            var (publisher, subscriber) = evFactory.CreateAsyncEvent<int>();

            var l = new List<int>();
            var d1 = subscriber.Subscribe(async (x, ct) => { await Task.Delay(500); lock (l) { l.Add(x); } });
            var d2 = subscriber.Subscribe(async (x, ct) => { await Task.Delay(300); lock (l) { l.Add(x * 10); } });

            await publisher.PublishAsync(10);
            await publisher.PublishAsync(20);

            publisher.Dispose();

            await publisher.PublishAsync(30);
            await publisher.PublishAsync(40);

            l.Should().Equal(100, 10, 200, 20);

            d1.Dispose();
            d2.Dispose();
        }
    }
}

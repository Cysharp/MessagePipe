using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePipe.Tests
{
    public class BufferedTest
    {
        [Fact]
        public void Sync()
        {
            var provider = TestHelper.BuildServiceProvider();

            var p = provider.GetRequiredService<IBufferedPublisher<IntClass>>();
            var s = provider.GetRequiredService<IBufferedSubscriber<IntClass>>();

            var l = new List<int>();
            using (s.Subscribe(x => l.Add(x.Value)))
            {
                l.Count.Should().Be(0);
            }

            p.Publish(new IntClass { Value = 9999 }); // set initial value

            using var d2 = s.Subscribe(x => l.Add(x.Value));

            l.Should().Equal(9999);
            p.Publish(new IntClass { Value = 333 });
            l.Should().Equal(9999, 333);

            using var d3 = s.Subscribe(x => l.Add(x.Value));
            l.Should().Equal(9999, 333, 333);
            p.Publish(new IntClass { Value = 11 });
            l.Should().Equal(9999, 333, 333, 11, 11);
            d3.Dispose();
            p.Publish(new IntClass { Value = 4 });
            l.Should().Equal(9999, 333, 333, 11, 11, 4);
        }

        [Fact]
        public async Task ASync()
        {
            var provider = TestHelper.BuildServiceProvider();

            var p = provider.GetRequiredService<IBufferedAsyncPublisher<IntClass>>();
            var s = provider.GetRequiredService<IBufferedAsyncSubscriber<IntClass>>();

            var l = new List<int>();
            using (await s.SubscribeAsync(async (x, ct) => { await Task.Yield(); lock (l) { l.Add(x.Value); } }))
            {
                l.Count.Should().Be(0);
            }

            await p.PublishAsync(new IntClass { Value = 9999 }); // set initial value

            using var d2 = await s.SubscribeAsync(async (x, ct) => { await Task.Yield(); lock (l) { l.Add(x.Value); } });

            l.Should().Equal(9999);
            await p.PublishAsync(new IntClass { Value = 333 });
            l.Should().Equal(9999, 333);

            using var d3 = await s.SubscribeAsync(async (x, ct) => { await Task.Yield(); lock (l) { l.Add(x.Value); } });
            l.Should().Equal(9999, 333, 333);
            await p.PublishAsync(new IntClass { Value = 11 });
            l.Should().Equal(9999, 333, 333, 11, 11);
            d3.Dispose();
            await p.PublishAsync(new IntClass { Value = 4 });
            l.Should().Equal(9999, 333, 333, 11, 11, 4);
        }

        [Fact]
        public void EventFactory()
        {
            var provider = TestHelper.BuildServiceProvider();
            var evFactory = provider.GetRequiredService<EventFactory>();

            var (publisher, subscriber) = evFactory.CreateBufferedEvent(9999);

            var l = new List<int>();
            var d1 = subscriber.Subscribe(x => l.Add(x));
            var d2 = subscriber.Subscribe(x => l.Add(x * 10));

            l.Should().Equal(9999, 99990);

            publisher.Publish(10);
            publisher.Publish(20);

            var d3 = subscriber.Subscribe(x => l.Add(x * 100));

            publisher.Dispose();

            publisher.Publish(30);
            publisher.Publish(40);

            l.Should().Equal(9999, 99990, 10, 100, 20, 200, 2000);

            d1.Dispose();
            d2.Dispose();
        }

#pragma warning disable CS1998

        [Fact]
        public async Task AsyncEventFactory()
        {
            var provider = TestHelper.BuildServiceProvider();
            var evFactory = provider.GetRequiredService<EventFactory>();

            var (publisher, subscriber) = evFactory.CreateBufferedAsyncEvent(9999);

            var l = new List<int>();
            var d1 = await subscriber.SubscribeAsync(async (x, ct) => l.Add(x));
            var d2 = await subscriber.SubscribeAsync(async (x, ct) => l.Add(x * 10));

            l.Should().Equal(9999, 99990);

            await publisher.PublishAsync(10);
            await publisher.PublishAsync(20);

            var d3 = await subscriber.SubscribeAsync(async (x, ct) => l.Add(x * 100));

            publisher.Dispose();

            await publisher.PublishAsync(30);
            await publisher.PublishAsync(40);

            l.Should().Equal(9999, 99990, 10, 100, 20, 200, 2000);

            d1.Dispose();
            d2.Dispose();
        }

#pragma warning restore CS1998
    }

    public class IntClass
    {
        public int Value { get; set; }
    }
}

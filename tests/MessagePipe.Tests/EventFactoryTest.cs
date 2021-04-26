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
    public class EventFactoryTest
    {
        [Fact]
        public void Tes()
        {
            var provider = TestHelper.BuildServiceProvider();

            var evFactory = provider.GetRequiredService<EventFactory>();

            var (publisher, subscriber) = evFactory.Create<int>();

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
    }


    public class MyClass
    {
        // You can use EventFactory instead of event.
        // public event Action<int> OnTick;

        IPublisher<int> tickPublisher;
        public ISubscriber<int> OnTick { get; }

        public MyClass(EventFactory eventFactory)
        {
            (tickPublisher, OnTick) = eventFactory.Create<int>();
        }

        int count;
        void Tick()
        {
            tickPublisher.Publish(count++);
        }
    }



}

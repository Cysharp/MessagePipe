using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FluentAssertions;

namespace MessagePipe.Tests
{
    public class GlobalMixFilterTest
    {


        [Fact]
        public void WithFilter()
        {
            var store = new DataStore();

            var provider = TestHelper.BuildServiceProvider3(options =>
            {
                options.EnableAutoRegistration = false;
                //options.AddGlobalMessageHandlerFilter<MyFilter<int>>(1200);
                options.AddGlobalMessageHandlerFilter(typeof(MyFilter<>), 1200);
            }, builder =>
            {
                builder.AddMessageHandlerFilter<MyFilter<int>>();
                builder.AddSingleton(store);
            });

            var pub = provider.GetRequiredService<IPublisher<int>>();
            var sub1 = provider.GetRequiredService<ISubscriber<int>>();

            var d1 = sub1.Subscribe(new MyHandler(store), new MyFilter<int>(store) { Order = -10 });

            pub.Publish(9999);

            store.Logs.Should().Equal(new[]
            {
                "Order:-10",
                "Order:999",
                "Order:1099",
                "Order:1200",
                "Handle:9999",
            });
        }


        [Fact]
        public void Request()
        {
            var store = new DataStore();

            var provider = TestHelper.BuildServiceProvider3(options =>
            {
                options.EnableAutoRegistration = false;
                options.AddGlobalRequestHandlerFilter<MyRequestHandlerFilter>(-1799);
            }, builder =>
            {
                builder.AddRequestHandler<MyRequestHandler>();
                builder.AddSingleton(store);
            });

            var handler = provider.GetRequiredService<IRequestHandler<int, int>>();

            var result = handler.Invoke(1999);
            result.Should().Be(19990);

            store.Logs.Should().Equal(new[]
            {
                "Order:-1799",
                "Order:999",
                "Order:1099",
                "Invoke:1999",
            });
        }

        [Fact]
        public void RequestAll()
        {
            var store = new DataStore();

            var provider = TestHelper.BuildServiceProvider3(options =>
            {
                options.EnableAutoRegistration = false;
                options.AddGlobalRequestHandlerFilter<MyRequestHandlerFilter>(-1799);
            }, builder =>
            {
                builder.AddRequestHandler<MyRequestHandler>();
                builder.AddRequestHandler<MyRequestHandler2>();
                builder.AddSingleton(store);
            });

            var handler = provider.GetRequiredService<IRequestAllHandler<int, int>>();

            var result = handler.InvokeAll(1999);
            result.Should().Equal(19990, 199900);

            store.Logs.Should().Equal(new[]
            {
                "Order:-1799",
                "Order:999",
                "Order:1099",
                "Invoke:1999",
                "Order:-1799",
                "Invoke2:1999",
            });
        }
    }


    public class DataStore
    {
        public List<string> Logs { get; set; }

        public DataStore()
        {
            Logs = new List<string>();
        }
    }


    public class MyFilter<T> : MessageHandlerFilter<T>
    {
        readonly DataStore store;

        public MyFilter(DataStore store)
        {
            this.store = store;
        }

        public override void Handle(T message, Action<T> next)
        {
            store.Logs.Add($"Order:{Order}");
            next(message);
        }
    }

    [MessageHandlerFilter(typeof(MyFilter<int>), Order = 999)]
    [MessageHandlerFilter(typeof(MyFilter<int>), Order = 1099)]
    public class MyHandler : IMessageHandler<int>
    {
        DataStore store;

        public MyHandler(DataStore store)
        {
            this.store = store;
        }

        public void Handle(int message)
        {
            store.Logs.Add("Handle:" + message);
        }
    }

    [RequestHandlerFilter(typeof(MyRequestHandlerFilter), Order = 999)]
    [RequestHandlerFilter(typeof(MyRequestHandlerFilter), Order = 1099)]
    public class MyRequestHandler : IRequestHandler<int, int>
    {
        readonly DataStore store;

        public MyRequestHandler(DataStore store)
        {
            this.store = store;
        }

        public int Invoke(int request)
        {
            store.Logs.Add("Invoke:" + request);
            return request * 10;
        }
    }

    public class MyRequestHandler2 : IRequestHandler<int, int>
    {
        readonly DataStore store;

        public MyRequestHandler2(DataStore store)
        {
            this.store = store;
        }

        public int Invoke(int request)
        {
            store.Logs.Add("Invoke2:" + request);
            return request * 100;
        }
    }

    public class MyRequestHandlerFilter : RequestHandlerFilter<int, int>
    {
        readonly DataStore store;

        public MyRequestHandlerFilter(DataStore store)
        {
            this.store = store;
        }

        public override int Invoke(int request, Func<int, int> next)
        {
            store.Logs.Add($"Order:{Order}");
            return next(request);
        }
    }
}

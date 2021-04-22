using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;

public class VContainerTest
{
    [Test]
    public void SimpelePush()
    {
        using var resolver = TestHelper.BuildContainer((options, builder) =>
        {
            builder.RegisterMessageBroker<int>(options);
        });

        var pub = resolver.Resolve<IPublisher<int>>();
        var sub1 = resolver.Resolve<ISubscriber<int>>();
        var sub2 = resolver.Resolve<ISubscriber<int>>();

        var list = new List<int>();
        var d1 = sub1.Subscribe(x => list.Add(x));
        var d2 = sub2.Subscribe(x => list.Add(x));

        pub.Publish(10);
        pub.Publish(20);
        CollectionAssert.AreEqual(list, new[] { 10, 10, 20, 20 });

        list.Clear();
        d1.Dispose();

        pub.Publish(99);
        CollectionAssert.AreEqual(list, new[] { 99 });
    }

    [UnityTest]
    public IEnumerator SimpleAsyncPush() => UniTask.ToCoroutine(async () =>
    {
        using var resolver = TestHelper.BuildContainer((options, builder) =>
        {
            builder.RegisterMessageBroker<int>(options);
        });

        var pub = resolver.Resolve<IAsyncPublisher<int>>();
        var sub1 = resolver.Resolve<IAsyncSubscriber<int>>();
        var sub2 = resolver.Resolve<IAsyncSubscriber<int>>();

        var list = new List<int>();
        var d1 = sub1.Subscribe(async (x, c) => { await UniTask.Yield(); list.Add(x); });
        var d2 = sub2.Subscribe(async (x, c) => { await UniTask.Yield(); list.Add(x); });

        await pub.PublishAsync(10);
        await pub.PublishAsync(20);
        CollectionAssert.AreEqual(list, new[] { 10, 10, 20, 20 });

        list.Clear();
        d1.Dispose();

        await pub.PublishAsync(99);
        CollectionAssert.AreEqual(list, new[] { 99 });
    });

    [Test]
    public void WithFilter()
    {
        var store = new DataStore();

        using var resolver = TestHelper.BuildContainer(options =>
        {
            options.AddGlobalMessageHandlerFilter<MyFilter<int>>(1200);
        }, (options, builder) =>
        {
            builder.RegisterMessageBroker<int>(options);
            builder.RegisterMessageHandlerFilter<MyFilter<int>>();
            builder.RegisterInstance(store);
        });

        var pub = resolver.Resolve<IPublisher<int>>();
        var sub1 = resolver.Resolve<ISubscriber<int>>();

        var d1 = sub1.Subscribe(new MyHandler(store), new MyFilter<int>(store) { Order = -10 });

        pub.Publish(9999);

        CollectionAssert.AreEqual(store.Logs, new[]
        {
            "Order:-10",
            "Order:999",
            "Order:1099",
            "Order:1200",
            "Handle:9999",
        });
    }

    [Test]
    public void Request()
    {
        var store = new DataStore();

        using var resolver = TestHelper.BuildContainer(options =>
        {
            options.AddGlobalRequestHandlerFilter<MyRequestHandlerFilter>(-1799);
        }, (options, builder) =>
        {
            builder.RegisterInstance(store);
            builder.RegisterRequestHandler<int, int, MyRequestHandler>(options);
        });

        var handler = resolver.Resolve<IRequestHandler<int, int>>();

        var result = handler.Invoke(1999);
        Assert.AreEqual(result, 19990);

        CollectionAssert.AreEqual(store.Logs, new[]
        {
            "Order:-1799",
            "Order:999",
            "Order:1099",
            "Invoke:1999",
        });
    }

    [Test]
    public void RequestAll()
    {
        var store = new DataStore();

        using var resolver = TestHelper.BuildContainer(options =>
        {
            // options.InstanceLifetime = InstanceLifetime.Scoped;
            options.AddGlobalRequestHandlerFilter<MyRequestHandlerFilter>(-1799);
        }, (options, builder) =>
        {
            builder.RegisterInstance(store);
            builder.RegisterRequestHandler<int, int, MyRequestHandler>(options);
            builder.RegisterRequestHandler<int, int, MyRequestHandler2>(options);
        });

        var handler = resolver.Resolve<IRequestAllHandler<int, int>>();

        var result = handler.InvokeAll(1999);
        CollectionAssert.AreEqual(result, new[] { 19990, 199900 });

        CollectionAssert.AreEqual(store.Logs, new[]
        {
            "Order:-1799",
            "Order:999",
            "Order:1099",
            "Invoke:1999",
            "Order:-1799",
            "Invoke2:1999",
        });
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
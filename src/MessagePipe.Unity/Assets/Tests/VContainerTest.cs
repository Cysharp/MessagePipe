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
        using var resolver = TestHelper.BuildVContainer((options, builder) =>
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
        using var resolver = TestHelper.BuildVContainer((options, builder) =>
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

        using var resolver = TestHelper.BuildVContainer(options =>
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

        using var resolver = TestHelper.BuildVContainer(options =>
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

        using var resolver = TestHelper.BuildVContainer(options =>
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


}
﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;
using MessagePipe;
using Cysharp.Threading.Tasks;
using UnityEngine.TestTools;
using System.Collections;

public class ZenjectTest
{
    [Test]
    public void SimpelePush()
    {
        var resolver = TestHelper.BuildZenject((options, builder) =>
        {
            builder.BindMessageBroker<int>(options, ZenjectScope.Single);
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
        var resolver = TestHelper.BuildZenject((options, builder) =>
        {
            builder.BindMessageBroker<int>(options, ZenjectScope.Single);
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

        var resolver = TestHelper.BuildZenject(options =>
       {
           options.AddGlobalMessageHandlerFilter<MyFilter<int>>(1200);
       }, (options, builder) =>
       {
           builder.BindMessageBroker<int>(options, ZenjectScope.Single);
           builder.BindMessageHandlerFilter<MyFilter<int>>();
           builder.BindInstance(store);
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

        var resolver = TestHelper.BuildZenject(options =>
       {
           options.AddGlobalRequestHandlerFilter<MyRequestHandlerFilter>(-1799);
       }, (options, builder) =>
       {
           builder.BindInstance(store);
           builder.BindRequestHandler<int, int, MyRequestHandler>(options, ZenjectScope.Single);
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

        var resolver = TestHelper.BuildZenject(options =>
       {
           // options.InstanceLifetime = InstanceLifetime.Scoped;
           options.AddGlobalRequestHandlerFilter<MyRequestHandlerFilter>(-1799);
       }, (options, builder) =>
       {
           builder.BindInstance(store);
           builder.BindRequestHandler<int, int, MyRequestHandler>(options, ZenjectScope.Single);
           builder.BindRequestHandler<int, int, MyRequestHandler2>(options, ZenjectScope.Single);
       });

        var handler = resolver.Resolve<IRequestAllHandler<int, int>>();

        var result = handler.InvokeAll(1999);
        CollectionAssert.AreEqual(new[] { 19990, 199900 }, result);

        CollectionAssert.AreEqual(new[]
        {
            "Order:-1799",
            "Order:999",
            "Order:1099",
            "Invoke:1999",
            "Order:-1799",
            "Invoke2:1999",
        }, store.Logs);
    }

    [Test]
    public void Buffered()
    {
        var provider = TestHelper.BuildZenject((options, builder) =>
        {
            builder.BindMessageBroker<IntClass>(options, ZenjectScope.Single);
        });

        var p = provider.Resolve<IBufferedPublisher<IntClass>>();
        var s = provider.Resolve<IBufferedSubscriber<IntClass>>();

        var l = new List<int>();
        using (var d1 = s.Subscribe(x => l.Add(x.Value)))
        {
            Assert.AreEqual(0, l.Count);
        }

        p.Publish(new IntClass { Value = 9999 }); // set initial value

        var d2 = s.Subscribe(x => l.Add(x.Value));

        CollectionAssert.AreEqual(new[] { 9999 }, l);
        p.Publish(new IntClass { Value = 333 });
        CollectionAssert.AreEqual(new[] { 9999, 333 }, l);

        var d3 = s.Subscribe(x => l.Add(x.Value));
        CollectionAssert.AreEqual(new[] { 9999, 333, 333 }, l);
        p.Publish(new IntClass { Value = 11 });
        CollectionAssert.AreEqual(new[] { 9999, 333, 333, 11, 11 }, l);
        d3.Dispose();
        p.Publish(new IntClass { Value = 4 });
        CollectionAssert.AreEqual(new[] { 9999, 333, 333, 11, 11, 4 }, l);
    }

    public class IntClass
    {
        public int Value { get; set; }
    }
}

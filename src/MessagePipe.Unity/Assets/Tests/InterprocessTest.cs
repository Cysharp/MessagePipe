#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using VContainer;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

public class InterprocessTest
{
    [UnityTest]
    public IEnumerator SimpleUdp() => UniTask.ToCoroutine(async () =>
    {
        var rootProvider = TestHelper.BuildVContainer(x =>
        {
        }, (options, builder) =>
        {
            var sc = builder.AsServiceCollection();
            var newOptions = sc.AddMessagePipeUdpInterprocess("127.0.0.1", 1192, x =>
            {
                x.InstanceLifetime = InstanceLifetime.Scoped;
            });
            sc.RegisterUpdInterprocessMessageBroker<int, int>(newOptions);
        });

        using (var provider = rootProvider)
        {
            var p1 = provider.Resolve<IDistributedPublisher<int, int>>();
            var s1 = provider.Resolve<IDistributedSubscriber<int, int>>();

            var list1 = new List<int>();
            var list2 = new List<int>();

            var t = s1.SubscribeAsync(10, async x => list1.Add(x));

            await t;

            await UniTask.Delay(100);
            await p1.PublishAsync(10, 9999);
            await p1.PublishAsync(10, 8888);
            await p1.PublishAsync(19, 7777);


            await UniTask.Delay(100);
            await p1.PublishAsync(10, 6666);
            await p1.PublishAsync(19, 5555);

            await UniTask.Delay(100);

            CollectionAssert.AreEqual(list1, new[] { 9999, 8888, 6666 });
        }
    });

    [UnityTest]
    public IEnumerator SimpleNamedPipe() => UniTask.ToCoroutine(async () =>
    {
        var rootProvider = TestHelper.BuildVContainer(x =>
        {
        }, (options, builder) =>
        {
            var sc = builder.AsServiceCollection();
            var newOptions = sc.AddMessagePipeNamedPipeInterprocess("foobar", x =>
            {
                x.InstanceLifetime = InstanceLifetime.Scoped;
            });
            sc.RegisterNamedPipeInterprocessMessageBroker<int, int>(newOptions);
        });

        using (var provider = rootProvider)
        {
            var p1 = provider.Resolve<IDistributedPublisher<int, int>>();
            var s1 = provider.Resolve<IDistributedSubscriber<int, int>>();

            var list1 = new List<int>();
            var list2 = new List<int>();

            var t = s1.SubscribeAsync(10, async x => list1.Add(x));

            await t;

            await UniTask.Delay(100);
            await p1.PublishAsync(10, 9999);
            await p1.PublishAsync(10, 8888);
            await p1.PublishAsync(19, 7777);


            await UniTask.Delay(100);
            await p1.PublishAsync(10, 6666);
            await p1.PublishAsync(19, 5555);

            await UniTask.Delay(100);

            CollectionAssert.AreEqual(list1, new[] { 9999, 8888, 6666 });
        }
    });
}

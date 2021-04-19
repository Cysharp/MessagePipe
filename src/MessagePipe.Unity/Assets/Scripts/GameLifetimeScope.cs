using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        var options = builder.RegisterMessagePipe();
        builder.RegisterMessageBroker<int>(options);

        builder.RegisterEntryPoint<MessagePipeDemo>(Lifetime.Singleton);
    }
}

public class MessagePipeDemo : VContainer.Unity.IStartable
{
    readonly IPublisher<int> publisher;
    readonly ISubscriber<int> subscriber;

    public MessagePipeDemo(IPublisher<int> publisher, ISubscriber<int> subscriber)
    {
        this.publisher = publisher;
        this.subscriber = subscriber;
    }

    public void Start()
    {
        var d = DisposableBag.CreateBuilder();
        subscriber.Subscribe(x => Debug.Log("P1:" + x)).AddTo(d);
        subscriber.Subscribe(x => Debug.Log("P2:" + x)).AddTo(d);

        publisher.Publish(10);
        publisher.Publish(20);
        publisher.Publish(30);

        var disposable = d.Build();
        disposable.Dispose();
    }
}











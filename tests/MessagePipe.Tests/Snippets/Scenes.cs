using System;
using MessagePipe;

public struct MyEvent { }

public class SceneA
{
    readonly IPublisher<MyEvent> publisher;

    public SceneA(IPublisher<MyEvent> publisher)
    {
        this.publisher = publisher;
    }

    void Send()
    {
        this.publisher.Publish(new MyEvent());
    }
}

public class SceneB
{
    readonly IDisposable disposable;

    public SceneB(ISubscriber<MyEvent> subscriber)
    {
        var bag = DisposableBag.CreateBuilder(); // composite disposable for manage subscription

        subscriber.Subscribe(_ => Console.WriteLine("here")).AddTo(bag);

        disposable = bag.Build();
    }

    void Close()
    {
        disposable.Dispose(); // unsubscribe event, all subscription **must** Dispose when completed
    }
}
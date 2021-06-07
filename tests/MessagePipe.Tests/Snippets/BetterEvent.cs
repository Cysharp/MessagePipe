using System;
using MessagePipe;

public class BetterEvent : IDisposable
{
    // using MessagePipe instead of C# event/Rx.Subject
    // store Publisher to private field(declare IDisposablePublisher/IDisposableAsyncPublisher)
    IDisposablePublisher<int> tickPublisher;

    // Subscriber is used from outside so public property
    public ISubscriber<int> OnTick { get; }

    public BetterEvent(EventFactory eventFactory)
    {
        // CreateEvent can deconstruct by tuple and set together
        (tickPublisher, OnTick) = eventFactory.CreateEvent<int>();

        // also create async event(IAsyncSubscriber) by `CreateAsyncEvent`
        // eventFactory.CreateAsyncEvent
    }

    int count;
    void Tick()
    {
        tickPublisher.Publish(count++);
    }

    public void Dispose()
    {
        // You can unsubscribe all from Publisher.
        tickPublisher.Dispose();
    }
}
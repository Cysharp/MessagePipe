using System;


namespace MessagePipe
{
    public static partial class SubscriberExtensions
    {
        public static IObservable<TMessage> AsObservable<TMessage>(this ISubscriber<TMessage> subscriber, params MessageHandlerFilter<TMessage>[] filters)
        {
            return new ObservableSubscriber<TMessage>(subscriber, filters);
        }

        public static IObservable<TMessage> AsObservable<TMessage>(this IBufferedSubscriber<TMessage> subscriber, params MessageHandlerFilter<TMessage>[] filters)
        {
            return new ObservableBufferedSubscriber<TMessage>(subscriber, filters);
        }

        public static IObservable<TMessage> AsObservable<TKey, TMessage>(this ISubscriber<TKey, TMessage> subscriber, TKey key, params MessageHandlerFilter<TMessage>[] filters)
            
        {
            return new ObservableSubscriber<TKey, TMessage>(key, subscriber, filters);
        }
    }

    internal sealed class ObservableSubscriber<TKey, TMessage> : IObservable<TMessage>
        
    {
        readonly TKey key;
        readonly ISubscriber<TKey, TMessage> subscriber;
        readonly MessageHandlerFilter<TMessage>[] filters;

        public ObservableSubscriber(TKey key, ISubscriber<TKey, TMessage> subscriber, MessageHandlerFilter<TMessage>[] filters)
        {
            this.key = key;
            this.subscriber = subscriber;
            this.filters = filters;
        }

        public IDisposable Subscribe(IObserver<TMessage> observer)
        {
            return subscriber.Subscribe(key, new ObserverMessageHandler<TMessage>(observer), filters);
        }
    }

    internal sealed class ObservableSubscriber<TMessage> : IObservable<TMessage>
    {
        readonly ISubscriber<TMessage> subscriber;
        readonly MessageHandlerFilter<TMessage>[] filters;

        public ObservableSubscriber(ISubscriber<TMessage> subscriber, MessageHandlerFilter<TMessage>[] filters)
        {
            this.subscriber = subscriber;
            this.filters = filters;
        }

        public IDisposable Subscribe(IObserver<TMessage> observer)
        {
            return subscriber.Subscribe(new ObserverMessageHandler<TMessage>(observer), filters);
        }
    }

    internal sealed class ObservableBufferedSubscriber<TMessage> : IObservable<TMessage>
    {
        readonly IBufferedSubscriber<TMessage> subscriber;
        readonly MessageHandlerFilter<TMessage>[] filters;

        public ObservableBufferedSubscriber(IBufferedSubscriber<TMessage> subscriber, MessageHandlerFilter<TMessage>[] filters)
        {
            this.subscriber = subscriber;
            this.filters = filters;
        }

        public IDisposable Subscribe(IObserver<TMessage> observer)
        {
            return subscriber.Subscribe(new ObserverMessageHandler<TMessage>(observer), filters);
        }
    }

    internal sealed class ObserverMessageHandler<TMessage> : IMessageHandler<TMessage>
    {
        readonly IObserver<TMessage> observer;

        public ObserverMessageHandler(IObserver<TMessage> observer)
        {
            this.observer = observer;
        }

        public void Handle(TMessage message)
        {
            observer.OnNext(message);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace MessagePipe
{
    public interface IMessageHandler<T>
    {
        void Handle(T message);
    }

    public interface IAsyncMessageHandler<T>
    {
        // use Task for WhenAll
        Task HandleAsync(T message, CancellationToken cancellationToken);
    }

    // Keyed

    public interface IPublisher<TKey, TMessage>
    {
        void Publish(TKey key, TMessage message);
    }

    public interface ISubscriber<TKey, TMessage>
    {
        public IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler);
    }

    public interface IAsyncPublisher<TKey, TMessage>
    {
        ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IAsyncSubscriber<TKey, TMessage>
    {
        public IDisposable Subscribe(TKey key, IAsyncMessageHandler<TMessage> asyncHandler);
    }

    // Keyless

    public interface IPublisher<TMessage>
    {
        void Publish(TMessage message);
    }

    public interface ISubscriber<TMessage>
    {
        public IDisposable Subscribe(IMessageHandler<TMessage> handler);
    }

    public interface IAsyncPublisher<TMessage>
    {
        ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IAsyncSubscriber<TMessage>
    {
        public IDisposable Subscribe(IAsyncMessageHandler<TMessage> asyncHandler);
    }

    // Extensions

    public static partial class MessageBrokerExtensions
    {
        public static IDisposable Subscribe<TKey, TMessage>(this ISubscriber<TKey, TMessage> subscriber, TKey key, Action<TMessage> handler)
        {
            return subscriber.Subscribe(key, new AnonymousMessageHandler<TMessage>(handler));
        }

        public static IDisposable Subscribe<TMessage>(this ISubscriber<TMessage> subscriber, Action<TMessage> handler)
        {
            return subscriber.Subscribe(new AnonymousMessageHandler<TMessage>(handler));
        }

        public static IObservable<TMessage> AsObservable<TKey, TMessage>(this ISubscriber<TKey, TMessage> subscriber, TKey key)
        {
            return new ObservableSubscriber<TKey, TMessage>(key, subscriber);
        }

        public static IObservable<TMessage> AsObservable<TMessage>(this ISubscriber<TMessage> subscriber)
        {
            return new ObservableSubscriber<TMessage>(subscriber);
        }

        // TODO:keyed async Subscribe

        public static IDisposable Subscribe<TMessage>(this IAsyncSubscriber<TMessage> subscriber, Func<TMessage, CancellationToken, Task> handler)
        {
            return subscriber.Subscribe(new AnonymousAsyncMessageHandler<TMessage>(handler));
        }

        sealed class AnonymousMessageHandler<TMessage> : IMessageHandler<TMessage>
        {
            readonly Action<TMessage> handler;

            public AnonymousMessageHandler(Action<TMessage> handler)
            {
                this.handler = handler;
            }

            public void Handle(TMessage message)
            {
                handler.Invoke(message);
            }
        }

        sealed class AnonymousAsyncMessageHandler<TMessage> : IAsyncMessageHandler<TMessage>
        {
            readonly Func<TMessage, CancellationToken, Task> handler;

            public AnonymousAsyncMessageHandler(Func<TMessage, CancellationToken, Task> handler)
            {
                this.handler = handler;
            }

            public Task HandleAsync(TMessage message, CancellationToken cancellationToken)
            {
                return handler.Invoke(message, cancellationToken);
            }
        }
    }

    internal sealed class ObservableSubscriber<TKey, TMessage> : IObservable<TMessage>
    {
        readonly TKey key;
        readonly ISubscriber<TKey, TMessage> subscriber;

        public ObservableSubscriber(TKey key, ISubscriber<TKey, TMessage> subscriber)
        {
            this.key = key;
            this.subscriber = subscriber;
        }

        public IDisposable Subscribe(IObserver<TMessage> observer)
        {
            return subscriber.Subscribe(key, new ObserverMessageHandler<TMessage>(observer));
        }
    }

    internal sealed class ObservableSubscriber<TMessage> : IObservable<TMessage>
    {
        readonly ISubscriber<TMessage> subscriber;

        public ObservableSubscriber(ISubscriber<TMessage> subscriber)
        {
            this.subscriber = subscriber;
        }

        public IDisposable Subscribe(IObserver<TMessage> observer)
        {
            return subscriber.Subscribe(new ObserverMessageHandler<TMessage>(observer));
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
using MessagePipe.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace MessagePipe
{
    // handler

    public interface IMessageHandler<T>
    {
        void Handle(T message);
    }

    public interface IAsyncMessageHandler<T>
    {
        ValueTask HandleAsync(T message, CancellationToken cancellationToken);
    }

    // Keyed

    public interface IPublisher<TKey, TMessage>
        where TKey : notnull
    {
        void Publish(TKey key, TMessage message);
    }

    public interface ISubscriber<TKey, TMessage>
        where TKey : notnull
    {
        public IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler);
    }

    public interface IAsyncPublisher<TKey, TMessage>
        where TKey : notnull
    {
        ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IAsyncSubscriber<TKey, TMessage>
        where TKey : notnull
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
        public IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter[] filters);
    }

    public interface IAsyncPublisher<TMessage>
    {
        void Publish(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IAsyncSubscriber<TMessage>
    {
        public IDisposable Subscribe(IAsyncMessageHandler<TMessage> asyncHandler, params AsyncMessageHandlerFilter[] filters);
    }

    // Extensions

    public static partial class MessageBrokerExtensions
    {
        // pubsub-keyless-sync

        public static IDisposable Subscribe<TMessage>(this ISubscriber<TMessage> subscriber, Action<TMessage> handler, params MessageHandlerFilter[] filters)
        {
            return subscriber.Subscribe(new AnonymousMessageHandler<TMessage>(handler), filters);
        }

        public static IDisposable Subscribe<TMessage>(this ISubscriber<TMessage> subscribe, Action<TMessage> handler, Func<TMessage, bool> predicate, params MessageHandlerFilter[] filters)
        {
            var predicateFilter = new PredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return subscribe.Subscribe(new AnonymousMessageHandler<TMessage>(handler), filters);
        }

        public static IObservable<TMessage> AsObservable<TMessage>(this ISubscriber<TMessage> subscriber, params MessageHandlerFilter[] filters)
        {
            return new ObservableSubscriber<TMessage>(subscriber, filters);
        }

        // pubsub-keyless-async

        public static IDisposable Subscribe<TMessage>(this IAsyncSubscriber<TMessage> subscriber, Func<TMessage, CancellationToken, ValueTask> handler, params AsyncMessageHandlerFilter[] filters)
        {
            return subscriber.Subscribe(new AnonymousAsyncMessageHandler<TMessage>(handler), filters);
        }

        public static IDisposable Subscribe<TMessage>(this IAsyncSubscriber<TMessage> subscribe, Func<TMessage, CancellationToken, ValueTask> handler, Func<TMessage, bool> predicate, params AsyncMessageHandlerFilter[] filters)
        {
            var predicateFilter = new AsyncPredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return subscribe.Subscribe(new AnonymousAsyncMessageHandler<TMessage>(handler), filters);
        }

        // TODO:key...


        public static IDisposable Subscribe<TKey, TMessage>(this ISubscriber<TKey, TMessage> subscriber, TKey key, Action<TMessage> handler)
            where TKey : notnull
        {
            return subscriber.Subscribe(key, new AnonymousMessageHandler<TMessage>(handler));
        }

        public static IObservable<TMessage> AsObservable<TKey, TMessage>(this ISubscriber<TKey, TMessage> subscriber, TKey key)
            where TKey : notnull
        {
            return new ObservableSubscriber<TKey, TMessage>(key, subscriber);
        }


        // TODO:keyed async Subscribe

        public static IDisposable Subscribe<TMessage>(this IAsyncSubscriber<TMessage> subscriber, Func<TMessage, CancellationToken, ValueTask> handler)
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
            readonly Func<TMessage, CancellationToken, ValueTask> handler;

            public AnonymousAsyncMessageHandler(Func<TMessage, CancellationToken, ValueTask> handler)
            {
                this.handler = handler;
            }

            public ValueTask HandleAsync(TMessage message, CancellationToken cancellationToken)
            {
                return handler.Invoke(message, cancellationToken);
            }
        }
    }

    internal sealed class ObservableSubscriber<TKey, TMessage> : IObservable<TMessage>
        where TKey : notnull
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
        readonly MessageHandlerFilter[] filters;

        public ObservableSubscriber(ISubscriber<TMessage> subscriber, MessageHandlerFilter[] filters)
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
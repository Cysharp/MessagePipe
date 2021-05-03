using MessagePipe.Internal;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    public static partial class SubscriberExtensions
    {
        // pubsub-keyless-sync

        public static IDisposable Subscribe<TMessage>(this ISubscriber<TMessage> subscriber, Action<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters)
        {
            return subscriber.Subscribe(new AnonymousMessageHandler<TMessage>(handler), filters);
        }

        public static IDisposable Subscribe<TMessage>(this ISubscriber<TMessage> subscriber, Action<TMessage> handler, Func<TMessage, bool> predicate, params MessageHandlerFilter<TMessage>[] filters)
        {
            var predicateFilter = new PredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return subscriber.Subscribe(new AnonymousMessageHandler<TMessage>(handler), filters);
        }

        public static IObservable<TMessage> AsObservable<TMessage>(this ISubscriber<TMessage> subscriber, params MessageHandlerFilter<TMessage>[] filters)
        {
            return new ObservableSubscriber<TMessage>(subscriber, filters);
        }

        public static IDisposable Subscribe<TMessage>(this IBufferedSubscriber<TMessage> subscriber, Action<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters)
        {
            return subscriber.Subscribe(new AnonymousMessageHandler<TMessage>(handler), filters);
        }

        public static IDisposable Subscribe<TMessage>(this IBufferedSubscriber<TMessage> subscriber, Action<TMessage> handler, Func<TMessage, bool> predicate, params MessageHandlerFilter<TMessage>[] filters)
        {
            var predicateFilter = new PredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return subscriber.Subscribe(new AnonymousMessageHandler<TMessage>(handler), filters);
        }

        public static IObservable<TMessage> AsObservable<TMessage>(this IBufferedSubscriber<TMessage> subscriber, params MessageHandlerFilter<TMessage>[] filters)
        {
            return new ObservableBufferedSubscriber<TMessage>(subscriber, filters);
        }


        // pubsub-keyless-async

        public static IDisposable Subscribe<TMessage>(this IAsyncSubscriber<TMessage> subscriber, Func<TMessage, CancellationToken, UniTask> handler, params AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            return subscriber.Subscribe(new AnonymousAsyncMessageHandler<TMessage>(handler), filters);
        }

        public static IDisposable Subscribe<TMessage>(this IAsyncSubscriber<TMessage> subscriber, Func<TMessage, CancellationToken, UniTask> handler, Func<TMessage, bool> predicate, params AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            var predicateFilter = new AsyncPredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return subscriber.Subscribe(new AnonymousAsyncMessageHandler<TMessage>(handler), filters);
        }

        public static UniTask<IDisposable> SubscribeAsync<TMessage>(this IBufferedAsyncSubscriber<TMessage> subscriber, Func<TMessage, CancellationToken, UniTask> handler, CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(subscriber, handler, Array.Empty<AsyncMessageHandlerFilter<TMessage>>(), cancellationToken);
        }

        public static UniTask<IDisposable> SubscribeAsync<TMessage>(this IBufferedAsyncSubscriber<TMessage> subscriber, Func<TMessage, CancellationToken, UniTask> handler, AsyncMessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            return subscriber.SubscribeAsync(new AnonymousAsyncMessageHandler<TMessage>(handler), filters, cancellationToken);
        }

        public static UniTask<IDisposable> SubscribeAsync<TMessage>(this IBufferedAsyncSubscriber<TMessage> subscriber, Func<TMessage, CancellationToken, UniTask> handler, Func<TMessage, bool> predicate, CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(subscriber, handler, predicate, Array.Empty<AsyncMessageHandlerFilter<TMessage>>(), cancellationToken);
        }

        public static UniTask<IDisposable> SubscribeAsync<TMessage>(this IBufferedAsyncSubscriber<TMessage> subscriber, Func<TMessage, CancellationToken, UniTask> handler, Func<TMessage, bool> predicate, AsyncMessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            var predicateFilter = new AsyncPredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return subscriber.SubscribeAsync(new AnonymousAsyncMessageHandler<TMessage>(handler), filters, cancellationToken);
        }

        // pubsub-key-sync

        public static IDisposable Subscribe<TKey, TMessage>(this ISubscriber<TKey, TMessage> subscriber, TKey key, Action<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters)
            
        {
            return subscriber.Subscribe(key, new AnonymousMessageHandler<TMessage>(handler), filters);
        }

        public static IDisposable Subscribe<TKey, TMessage>(this ISubscriber<TKey, TMessage> subscriber, TKey key, Action<TMessage> handler, Func<TMessage, bool> predicate, params MessageHandlerFilter<TMessage>[] filters)
            
        {
            var predicateFilter = new PredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return subscriber.Subscribe(key, new AnonymousMessageHandler<TMessage>(handler), filters);
        }

        public static IObservable<TMessage> AsObservable<TKey, TMessage>(this ISubscriber<TKey, TMessage> subscriber, TKey key, params MessageHandlerFilter<TMessage>[] filters)
            
        {
            return new ObservableSubscriber<TKey, TMessage>(key, subscriber, filters);
        }

        // pubsub-key-async

        public static IDisposable Subscribe<TKey, TMessage>(this IAsyncSubscriber<TKey, TMessage> subscriber, TKey key, Func<TMessage, CancellationToken, UniTask> handler, params AsyncMessageHandlerFilter<TMessage>[] filters)
            
        {
            return subscriber.Subscribe(key, new AnonymousAsyncMessageHandler<TMessage>(handler), filters);
        }

        public static IDisposable Subscribe<TKey, TMessage>(this IAsyncSubscriber<TKey, TMessage> subscriber, TKey key, Func<TMessage, CancellationToken, UniTask> handler, Func<TMessage, bool> predicate, params AsyncMessageHandlerFilter<TMessage>[] filters)
            
        {
            var predicateFilter = new AsyncPredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return subscriber.Subscribe(key, new AnonymousAsyncMessageHandler<TMessage>(handler), filters);
        }
    }

    internal sealed class AnonymousMessageHandler<TMessage> : IMessageHandler<TMessage>
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

    internal sealed class AnonymousAsyncMessageHandler<TMessage> : IAsyncMessageHandler<TMessage>
    {
        readonly Func<TMessage, CancellationToken, UniTask> handler;

        public AnonymousAsyncMessageHandler(Func<TMessage, CancellationToken, UniTask> handler)
        {
            this.handler = handler;
        }

        public UniTask HandleAsync(TMessage message, CancellationToken cancellationToken)
        {
            return handler.Invoke(message, cancellationToken);
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
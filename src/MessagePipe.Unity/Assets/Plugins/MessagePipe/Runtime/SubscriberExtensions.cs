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
}
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    public static class DistributedSubscriberExtensions
    {
        // sync handler

        public static UniTask<IUniTaskAsyncDisposable> SubscribeAsync<TKey, TMessage>(this IDistributedSubscriber<TKey, TMessage> subscriber, TKey key, Action<TMessage> handler, CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(subscriber, key, handler, Array.Empty<MessageHandlerFilter<TMessage>>(), cancellationToken);
        }

        public static UniTask<IUniTaskAsyncDisposable> SubscribeAsync<TKey, TMessage>(this IDistributedSubscriber<TKey, TMessage> subscriber, TKey key, Action<TMessage> handler, MessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            return subscriber.SubscribeAsync(key, new AnonymousMessageHandler<TMessage>(handler), filters, cancellationToken);
        }

        public static UniTask<IUniTaskAsyncDisposable> SubscribeAsync<TKey, TMessage>(this IDistributedSubscriber<TKey, TMessage> subscriber, TKey key, Action<TMessage> handler, Func<TMessage, bool> predicate, CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(subscriber, key, handler, predicate, Array.Empty<MessageHandlerFilter<TMessage>>(), cancellationToken);
        }

        public static UniTask<IUniTaskAsyncDisposable> SubscribeAsync<TKey, TMessage>(this IDistributedSubscriber<TKey, TMessage> subscriber, TKey key, Action<TMessage> handler, Func<TMessage, bool> predicate, MessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            var predicateFilter = new PredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : Append(filters, predicateFilter);

            return subscriber.SubscribeAsync(key, new AnonymousMessageHandler<TMessage>(handler), filters, cancellationToken);
        }

        // async handler

        public static UniTask<IUniTaskAsyncDisposable> SubscribeAsync<TKey, TMessage>(this IDistributedSubscriber<TKey, TMessage> subscriber, TKey key, Func<TMessage, CancellationToken, UniTask> handler, CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(subscriber, key, handler, Array.Empty<AsyncMessageHandlerFilter<TMessage>>(), cancellationToken);
        }

        public static UniTask<IUniTaskAsyncDisposable> SubscribeAsync<TKey, TMessage>(this IDistributedSubscriber<TKey, TMessage> subscriber, TKey key, Func<TMessage, CancellationToken, UniTask> handler, AsyncMessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            return subscriber.SubscribeAsync(key, new AnonymousAsyncMessageHandler<TMessage>(handler), filters, cancellationToken);
        }

        public static UniTask<IUniTaskAsyncDisposable> SubscribeAsync<TKey, TMessage>(this IDistributedSubscriber<TKey, TMessage> subscriber, TKey key, Func<TMessage, CancellationToken, UniTask> handler, Func<TMessage, bool> predicate, CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(subscriber, key, handler, predicate, Array.Empty<AsyncMessageHandlerFilter<TMessage>>(), cancellationToken);
        }

        public static UniTask<IUniTaskAsyncDisposable> SubscribeAsync<TKey, TMessage>(this IDistributedSubscriber<TKey, TMessage> subscriber, TKey key, Func<TMessage, CancellationToken, UniTask> handler, Func<TMessage, bool> predicate, AsyncMessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            var predicateFilter = new AsyncPredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : Append(filters, predicateFilter);

            return subscriber.SubscribeAsync(key, new AnonymousAsyncMessageHandler<TMessage>(handler), filters, cancellationToken);
        }

        static T[] Append<T>(T[] source, T item)
        {
            var dest = new T[source.Length + 1];
            Array.Copy(source, 0, dest, 0, source.Length);
            dest[dest.Length - 1] = item;
            return dest;
        }
    }
}
using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    public sealed class AsyncMessageBroker<TKey, TMessage> : IAsyncPublisher<TKey, TMessage>, IAsyncSubscriber<TKey, TMessage>
        where TKey : notnull
    {
        readonly AsyncMessageBrokerCore<TKey, TMessage> core;
        readonly MessagePipeOptions options;
        readonly FilterCache<AsyncMessageHandlerFilterAttribute, AsyncMessageHandlerFilter> filterCache;
        readonly IServiceProvider provider;

        public AsyncMessageBroker(AsyncMessageBrokerCore<TKey, TMessage> core, MessagePipeOptions options, FilterCache<AsyncMessageHandlerFilterAttribute, AsyncMessageHandlerFilter> filterCache, IServiceProvider provider)
        {
            this.core = core;
            this.options = options;
            this.filterCache = filterCache;
            this.provider = provider;
        }

        public void Publish(TKey key, TMessage message, CancellationToken cancellationToken)
        {
            core.Publish(key, message, cancellationToken);
        }

        public ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default)
        {
            return core.PublishAsync(key, message, options.DefaultAsyncPublishStrategy, cancellationToken);
        }

        public ValueTask PublishAsync(TKey key, TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default)
        {
            return core.PublishAsync(key, message, publishStrategy, cancellationToken);
        }

        public IDisposable Subscribe(TKey key, IAsyncMessageHandler<TMessage> handler, params AsyncMessageHandlerFilter[] filters)
        {
            var handlerFilters = filterCache.GetOrAddFilters(handler.GetType(), provider);
            var globalFilters = options.GetGlobalAsyncMessageHandlerFilters(provider);

            if (filters.Length != 0 || handlerFilters.Length != 0 || globalFilters.Length != 0)
            {
                handler = new FilterAttachedAsyncMessageHandler<TMessage>(handler, ArrayUtil.Concat(filters, handlerFilters, globalFilters));
            }

            return core.Subscribe(key, handler);
        }
    }

    public sealed class AsyncMessageBrokerCore<TKey, TMessage> : IDisposable
        where TKey : notnull
    {
        readonly Dictionary<TKey, HandlerHolder> handlerGroup;
        readonly MessagePipeDiagnosticsInfo diagnotics;
        readonly object gate;
        bool isDisposed;

        public AsyncMessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics)
        {
            this.handlerGroup = new Dictionary<TKey, HandlerHolder>();
            this.diagnotics = diagnotics;
            this.gate = new object();
        }

        public void Publish(TKey key, TMessage message, CancellationToken cancellationToken)
        {
            IAsyncMessageHandler<TMessage>?[] handlers;
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    return;
                }
                handlers = holder.GetHandlers();
            }

            for (int i = 0; i < handlers.Length; i++)
            {
                handlers[i]?.HandleAsync(message, cancellationToken).Forget();
            }
        }

        public async ValueTask PublishAsync(TKey key, TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken)
        {
            IAsyncMessageHandler<TMessage>?[] handlers;
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    return;
                }
                handlers = holder.GetHandlers();
            }

            if (publishStrategy == AsyncPublishStrategy.Sequential)
            {
                foreach (var item in handlers)
                {
                    if (item != null)
                    {
                        await item.HandleAsync(message, cancellationToken);
                    }
                }
            }
            else
            {
                await new AsyncHandlerWhenAll<TMessage>(handlers, message, cancellationToken);
            }
        }

        public IDisposable Subscribe(TKey key, IAsyncMessageHandler<TMessage> handler)
        {
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(key, out var holder))
                {
                    handlerGroup[key] = holder = new HandlerHolder(this);
                }

                return holder.Subscribe(key, handler);
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    foreach (var handlers in handlerGroup.Values)
                    {
                        handlers.Dispose();
                    }
                }
            }
        }

        // similar as Keyless-MessageBrokerCore but require to remove when key is empty on Dispose
        sealed class HandlerHolder : IDisposable, IHandlerHolderMarker
        {
            readonly FreeList<IAsyncMessageHandler<TMessage>> handlers;
            readonly AsyncMessageBrokerCore<TKey, TMessage> core;

            public HandlerHolder(AsyncMessageBrokerCore<TKey, TMessage> core)
            {
                this.handlers = new FreeList<IAsyncMessageHandler<TMessage>>();
                this.core = core;
            }

            public IAsyncMessageHandler<TMessage>?[] GetHandlers() => handlers.GetValues();

            public IDisposable Subscribe(TKey key, IAsyncMessageHandler<TMessage> handler)
            {
                var subscriptionKey = handlers.Add(handler);
                var subscription = new Subscription(key, subscriptionKey, this);
                core.diagnotics.IncrementSubscribe(this, subscription);
                return subscription;
            }

            public void Dispose()
            {
                lock (core.gate)
                {
                    if (handlers.TryDispose(out var count))
                    {
                        core.diagnotics.RemoveTargetDiagnostics(this, count);
                    }
                }
            }

            sealed class Subscription : IDisposable
            {
                bool isDisposed;
                readonly TKey key;
                readonly int subscriptionKey;
                readonly HandlerHolder holder;

                public Subscription(TKey key, int subscriptionKey, HandlerHolder holder)
                {
                    this.key = key;
                    this.subscriptionKey = subscriptionKey;
                    this.holder = holder;
                }

                public void Dispose()
                {
                    if (!isDisposed)
                    {
                        isDisposed = true;
                        lock (holder.core.gate)
                        {
                            if (!holder.core.isDisposed)
                            {
                                holder.handlers.Remove(subscriptionKey, false);
                                holder.core.diagnotics.DecrementSubscribe(holder, this);
                                if (holder.handlers.GetCount() == 0)
                                {
                                    holder.core.handlerGroup.Remove(key);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
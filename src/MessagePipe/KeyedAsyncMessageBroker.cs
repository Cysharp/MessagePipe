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

    public sealed class AsyncMessageBrokerCore<TKey, TMessage>
        where TKey : notnull
    {
        readonly Dictionary<TKey, HandlerHolder> handlerGroup;
        readonly MessagePipeDiagnosticsInfo diagnotics;
        readonly object gate;

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
                    handlerGroup[key] = holder = new HandlerHolder();
                }

                return holder.Subscribe(this, key, handler);
            }
        }

        // similar as Keyless-MessageBrokerCore but require to remove when key is empty on Dispose
        sealed class HandlerHolder
        {
            readonly FreeList<IDisposable, IAsyncMessageHandler<TMessage>> handlers;

            public HandlerHolder()
            {
                this.handlers = new FreeList<IDisposable, IAsyncMessageHandler<TMessage>>();
            }

            public IAsyncMessageHandler<TMessage>?[] GetHandlers() => handlers.GetUnsafeRawItems();

            public IDisposable Subscribe(AsyncMessageBrokerCore<TKey, TMessage> core, TKey key, IAsyncMessageHandler<TMessage> handler)
            {
                var subscription = new Subscription(core, key, this);
                handlers.Add(subscription, handler);
                core.diagnotics.IncrementSubscribe(subscription);
                return subscription;
            }

            sealed class Subscription : IDisposable
            {
                bool isDisposed;
                readonly AsyncMessageBrokerCore<TKey, TMessage> core;
                readonly TKey key;
                readonly HandlerHolder holder;

                public Subscription(AsyncMessageBrokerCore<TKey, TMessage> core, TKey key, HandlerHolder holder)
                {
                    this.core = core;
                    this.key = key;
                    this.holder = holder;
                }

                public void Dispose()
                {
                    if (!isDisposed)
                    {
                        isDisposed = true;
                        lock (core.gate)
                        {
                            holder.handlers.Remove(this);
                            core.diagnotics.DecrementSubscribe(this);

                            if (holder.handlers.Count == 0)
                            {
                                core.handlerGroup.Remove(key);
                            }
                        }
                    }
                }
            }
        }
    }
}
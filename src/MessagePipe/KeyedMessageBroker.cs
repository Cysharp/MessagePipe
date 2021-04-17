using MessagePipe.Internal;
using System;
using System.Collections.Generic;

namespace MessagePipe
{
    public sealed class MessageBroker<TKey, TMessage> : IPublisher<TKey, TMessage>, ISubscriber<TKey, TMessage>
        where TKey : notnull
    {
        readonly MessageBrokerCore<TKey, TMessage> core;
        readonly MessagePipeOptions options;
        readonly FilterCache<MessageHandlerFilterAttribute, MessageHandlerFilter> filterCache;
        readonly IServiceProvider provider;

        public MessageBroker(MessageBrokerCore<TKey, TMessage> core, MessagePipeOptions options, FilterCache<MessageHandlerFilterAttribute, MessageHandlerFilter> filterCache, IServiceProvider provider)
        {
            this.core = core;
            this.options = options;
            this.filterCache = filterCache;
            this.provider = provider;
        }

        public void Publish(TKey key, TMessage message)
        {
            core.Publish(key, message);
        }

        public IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler, params MessageHandlerFilter[] filters)
        {
            var handlerFilters = filterCache.GetOrAddFilters(handler.GetType(), provider);
            var globalFilters = options.GetGlobalMessageHandlerFilters(provider);

            if (filters.Length != 0 || handlerFilters.Length != 0 || globalFilters.Length != 0)
            {
                handler = new FilterAttachedMessageHandler<TMessage>(handler, ArrayUtil.Concat(filters, handlerFilters, globalFilters));
            }

            return core.Subscribe(key, handler);
        }
    }

    public sealed class MessageBrokerCore<TKey, TMessage>
        where TKey : notnull
    {
        readonly Dictionary<TKey, HandlerHolder> handlerGroup;
        readonly MessagePipeDiagnosticsInfo diagnotics;
        readonly object gate;

        public MessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics)
        {
            this.handlerGroup = new Dictionary<TKey, HandlerHolder>();
            this.diagnotics = diagnotics;
            this.gate = new object();
        }

        public void Publish(TKey id, TMessage message)
        {
            IMessageHandler<TMessage>?[] handlers;
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(id, out var holder))
                {
                    return;
                }
                handlers = holder.GetHandlers();
            }

            for (int i = 0; i < handlers.Length; i++)
            {
                handlers[i]?.Handle(message);
            }
        }

        public IDisposable Subscribe(TKey id, IMessageHandler<TMessage> handler)
        {
            lock (gate)
            {
                if (!handlerGroup.TryGetValue(id, out var holder))
                {
                    handlerGroup[id] = holder = new HandlerHolder();
                }

                return holder.Subscribe(this, id, handler);
            }
        }

        // similar as Keyless-MessageBrokerCore but require to remove when key is empty on Dispose
        sealed class HandlerHolder
        {
            readonly FreeList<IDisposable, IMessageHandler<TMessage>> handlers;

            public HandlerHolder()
            {
                this.handlers = new FreeList<IDisposable, IMessageHandler<TMessage>>();
            }

            public IMessageHandler<TMessage>?[] GetHandlers() => handlers.GetUnsafeRawItems();

            public IDisposable Subscribe(MessageBrokerCore<TKey, TMessage> core, TKey key, IMessageHandler<TMessage> handler)
            {
                var subscription = new Subscription(core, key, this);
                handlers.Add(subscription, handler);
                core.diagnotics.IncrementSubscribe(subscription);
                return subscription;
            }

            sealed class Subscription : IDisposable
            {
                bool isDisposed;
                readonly MessageBrokerCore<TKey, TMessage> core;
                readonly TKey id;
                readonly HandlerHolder holder;

                public Subscription(MessageBrokerCore<TKey, TMessage> core, TKey id, HandlerHolder holder)
                {
                    this.core = core;
                    this.id = id;
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
                                core.handlerGroup.Remove(id);
                            }
                        }
                    }
                }
            }
        }
    }
}
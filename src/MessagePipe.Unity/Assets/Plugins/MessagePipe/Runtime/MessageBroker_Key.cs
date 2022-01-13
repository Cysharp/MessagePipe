using MessagePipe.Internal;
using System;
using System.Collections.Generic;

namespace MessagePipe
{
    [Preserve]
    public class MessageBroker<TKey, TMessage> : IPublisher<TKey, TMessage>, ISubscriber<TKey, TMessage>
        
    {
        readonly MessageBrokerCore<TKey, TMessage> core;
        readonly FilterAttachedMessageHandlerFactory handlerFactory;

        [Preserve]
        public MessageBroker(MessageBrokerCore<TKey, TMessage> core, FilterAttachedMessageHandlerFactory handlerFactory)
        {
            this.core = core;
            this.handlerFactory = handlerFactory;
        }

        public void Publish(TKey key, TMessage message)
        {
            core.Publish(key, message);
        }

        public IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters)
        {
            return core.Subscribe(key, handlerFactory.CreateMessageHandler(handler, filters));
        }
    }

    [Preserve]
    public class MessageBrokerCore<TKey, TMessage> : IDisposable
    
    {
        readonly Dictionary<TKey, HandlerHolder> handlerGroup;
        readonly MessagePipeDiagnosticsInfo diagnotics;
        readonly HandlingSubscribeDisposedPolicy handlingSubscribeDisposedPolicy;
        readonly object gate;
        bool isDisposed;

        [Preserve]
        public MessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics, MessagePipeOptions options)
        {
            this.handlerGroup = new Dictionary<TKey, HandlerHolder>();
            this.diagnotics = diagnotics;
            this.handlingSubscribeDisposedPolicy = options.HandlingSubscribeDisposedPolicy;
            this.gate = new object();
        }

        public void Publish(TKey key, TMessage message)
        {
            IMessageHandler<TMessage>[] handlers;
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
                handlers[i]?.Handle(message);
            }
        }

        public IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler)
        {
            lock (gate)
            {
                if (isDisposed) return handlingSubscribeDisposedPolicy.Handle(nameof(MessageBrokerCore<TKey, TMessage>));

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
            readonly FreeList<IMessageHandler<TMessage>> handlers;
            readonly MessageBrokerCore<TKey, TMessage> core;

            public HandlerHolder(MessageBrokerCore<TKey, TMessage> core)
            {
                this.handlers = new FreeList<IMessageHandler<TMessage>>();
                this.core = core;
            }

            public IMessageHandler<TMessage>[] GetHandlers() => handlers.GetValues();

            public IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler)
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

    // Singleton, Scoped variation

    [Preserve]
    public class SingletonMessageBroker<TKey, TMessage> : MessageBroker<TKey, TMessage>, ISingletonPublisher<TKey, TMessage>, ISingletonSubscriber<TKey, TMessage>
        
    {
        public SingletonMessageBroker(SingletonMessageBrokerCore<TKey, TMessage> core, FilterAttachedMessageHandlerFactory handlerFactory)
            : base(core, handlerFactory)
        {
        }
    }

    [Preserve]
    public class SingletonMessageBrokerCore<TKey, TMessage> : MessageBrokerCore<TKey, TMessage>
        
    {
        public SingletonMessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics, MessagePipeOptions options)
            : base(diagnotics, options)
        {
        }
    }

    [Preserve]
    public class ScopedMessageBroker<TKey, TMessage> : MessageBroker<TKey, TMessage>, IScopedPublisher<TKey, TMessage>, IScopedSubscriber<TKey, TMessage>
        
    {
        public ScopedMessageBroker(ScopedMessageBrokerCore<TKey, TMessage> core, FilterAttachedMessageHandlerFactory handlerFactory)
            : base(core, handlerFactory)
        {
        }
    }

    [Preserve]
    public class ScopedMessageBrokerCore<TKey, TMessage> : MessageBrokerCore<TKey, TMessage>
        
    {
        public ScopedMessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics, MessagePipeOptions options)
            : base(diagnotics, options)
        {
        }
    }
}
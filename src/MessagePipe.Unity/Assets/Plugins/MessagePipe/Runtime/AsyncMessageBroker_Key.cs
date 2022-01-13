using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    [Preserve]
    public class AsyncMessageBroker<TKey, TMessage> : IAsyncPublisher<TKey, TMessage>, IAsyncSubscriber<TKey, TMessage>
        
    {
        readonly AsyncMessageBrokerCore<TKey, TMessage> core;
        readonly FilterAttachedAsyncMessageHandlerFactory handlerFactory;

        [Preserve]
        public AsyncMessageBroker(AsyncMessageBrokerCore<TKey, TMessage> core, FilterAttachedAsyncMessageHandlerFactory handlerFactory)
        {
            this.core = core;
            this.handlerFactory = handlerFactory;
        }

        public void Publish(TKey key, TMessage message, CancellationToken cancellationToken)
        {
            core.Publish(key, message, cancellationToken);
        }

        public UniTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken)
        {
            return core.PublishAsync(key, message, cancellationToken);
        }

        public UniTask PublishAsync(TKey key, TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken)
        {
            return core.PublishAsync(key, message, publishStrategy, cancellationToken);
        }

        public IDisposable Subscribe(TKey key, IAsyncMessageHandler<TMessage> handler, params AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            return core.Subscribe(key, handlerFactory.CreateAsyncMessageHandler(handler, filters));
        }
    }

    [Preserve]
    public class AsyncMessageBrokerCore<TKey, TMessage> : IDisposable
        
    {
        readonly Dictionary<TKey, HandlerHolder> handlerGroup;
        readonly MessagePipeDiagnosticsInfo diagnotics;
        readonly AsyncPublishStrategy defaultAsyncPublishStrategy;
        readonly HandlingSubscribeDisposedPolicy handlingSubscribeDisposedPolicy;
        readonly object gate;
        bool isDisposed;

        [Preserve]
        public AsyncMessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics, MessagePipeOptions options)
        {
            this.handlerGroup = new Dictionary<TKey, HandlerHolder>();
            this.diagnotics = diagnotics;
            this.defaultAsyncPublishStrategy = options.DefaultAsyncPublishStrategy;
            this.handlingSubscribeDisposedPolicy = options.HandlingSubscribeDisposedPolicy;
            this.gate = new object();
        }

        public void Publish(TKey key, TMessage message, CancellationToken cancellationToken)
        {
            IAsyncMessageHandler<TMessage>[] handlers;
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

        public UniTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken)
        {
            return PublishAsync(key, message, defaultAsyncPublishStrategy, cancellationToken);
        }

        public async UniTask PublishAsync(TKey key, TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken)
        {
            IAsyncMessageHandler<TMessage>[] handlers;
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
                if (isDisposed) return handlingSubscribeDisposedPolicy.Handle(nameof(AsyncMessageBrokerCore<TKey, TMessage>));

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

            public IAsyncMessageHandler<TMessage>[] GetHandlers() => handlers.GetValues();

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

    // Singleton, Scoped variation

    [Preserve]
    public class SingletonAsyncMessageBroker<TKey, TMessage> : AsyncMessageBroker<TKey, TMessage>, ISingletonAsyncPublisher<TKey, TMessage>, ISingletonAsyncSubscriber<TKey, TMessage>
        
    {
        public SingletonAsyncMessageBroker(SingletonAsyncMessageBrokerCore<TKey, TMessage> core, FilterAttachedAsyncMessageHandlerFactory handlerFactory)
            : base(core, handlerFactory)
        {
        }
    }

    [Preserve]
    public class SingletonAsyncMessageBrokerCore<TKey, TMessage> : AsyncMessageBrokerCore<TKey, TMessage>
        
    {
        public SingletonAsyncMessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics, MessagePipeOptions options)
            : base(diagnotics, options)
        {
        }
    }

    [Preserve]
    public class ScopedAsyncMessageBroker<TKey, TMessage> : AsyncMessageBroker<TKey, TMessage>, IScopedAsyncPublisher<TKey, TMessage>, IScopedAsyncSubscriber<TKey, TMessage>
        
    {
        public ScopedAsyncMessageBroker(ScopedAsyncMessageBrokerCore<TKey, TMessage> core, FilterAttachedAsyncMessageHandlerFactory handlerFactory)
            : base(core, handlerFactory)
        {
        }
    }

    [Preserve]
    public class ScopedAsyncMessageBrokerCore<TKey, TMessage> : AsyncMessageBrokerCore<TKey, TMessage>
        
    {
        public ScopedAsyncMessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics, MessagePipeOptions options)
            : base(diagnotics, options)
        {
        }
    }
}
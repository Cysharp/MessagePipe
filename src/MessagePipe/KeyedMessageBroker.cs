using MessageBroker.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MessagePipe
{
    public interface IMessageBroker<TKey, TMessage> : IPublisher<TKey, TMessage>, ISubscriber<TKey, TMessage>
    {
    }

    public sealed class MessageBroker<TKey, TMessage> : IPublisher<TKey, TMessage>, ISubscriber<TKey, TMessage>
    {
        readonly IMessageBroker<TKey, TMessage> core;

        public MessageBroker(IMessageBroker<TKey, TMessage> core)
        {
            this.core = core;
        }

        public void Publish(TKey key, TMessage message)
        {
            core.Publish(key, message);
        }

        public IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler)
        {
            return core.Subscribe(key, handler);
        }
    }

    internal sealed class ConcurrentDictionaryMessageBroker<TKey, TMessage> : IMessageBroker<TKey, TMessage>
    {
        // TODO: use logger?
        readonly ILogger<ConcurrentDictionaryMessageBroker<TKey, TMessage>> logger;
        readonly Dictionary<TKey, SubscriptionHolder> subscriptions;
        readonly object gate;

        public ConcurrentDictionaryMessageBroker(ILogger<ConcurrentDictionaryMessageBroker<TKey, TMessage>> logger)
        {
            this.logger = logger;
            this.subscriptions = new Dictionary<TKey, SubscriptionHolder>();
            this.gate = new object();
        }

        public void Publish(TKey id, TMessage message)
        {
            SubscriptionHolder holder;
            lock (gate)
            {
                if (!subscriptions.TryGetValue(id, out holder!))
                {
                    return;
                }
            }

            // outside from lock.
            using (var e = holder.GetHandlers())
            {
                while (e.MoveNext())
                {
                    // TODO: error handle strategy?
                    e.Current.Value.Handle(message);
                }
            }
        }

        public IDisposable Subscribe(TKey id, IMessageHandler<TMessage> handler)
        {
            lock (gate)
            {
                if (!subscriptions.TryGetValue(id, out var holder))
                {
                    subscriptions[id] = holder = new SubscriptionHolder();
                }

                return holder.RegisterHandler(this, id, handler);
            }
        }

        sealed class SubscriptionHolder
        {
            // ConcurrentDictionary.Count is slow, should not use.
            int handlerCount;
            readonly ConcurrentDictionary<IDisposable, IMessageHandler<TMessage>> handlers;

            public SubscriptionHolder()
            {
                handlerCount = 0;
                handlers = new ConcurrentDictionary<IDisposable, IMessageHandler<TMessage>>();
            }

            public IEnumerator<KeyValuePair<IDisposable, IMessageHandler<TMessage>>> GetHandlers()
            {
                // .Values allocate ICollection<> so should avoid it.
                return handlers.GetEnumerator();
            }

            public IDisposable RegisterHandler(ConcurrentDictionaryMessageBroker<TKey, TMessage> core, TKey TKey, IMessageHandler<TMessage> handler)
            {
                lock (core.gate)
                {
                    var subscription = new Subscription(core, TKey, this);
                    handlers.TryAdd(subscription, handler);
                    handlerCount += 1;
                    return subscription;
                }
            }

            sealed class Subscription : IDisposable
            {
                readonly ConcurrentDictionaryMessageBroker<TKey, TMessage> core;
                readonly TKey id;
                readonly SubscriptionHolder holder;

                public Subscription(ConcurrentDictionaryMessageBroker<TKey, TMessage> core, TKey id, SubscriptionHolder holder)
                {
                    this.core = core;
                    this.id = id;
                    this.holder = holder;
                }

                public void Dispose()
                {
                    lock (core.gate)
                    {
                        holder.handlers.TryRemove(this, out _);
                        holder.handlerCount -= 1;

                        // remove from root when parent is empty.
                        if (holder.handlerCount == 0)
                        {
                            core.subscriptions.Remove(id);
                        }
                    }
                }
            }
        }
    }

    internal sealed class ImmutableArrayMessageBroker<TKey, TMessage> : IMessageBroker<TKey, TMessage>
    {
        // TODO: use logger?
        readonly ILogger<ImmutableArrayMessageBroker<TKey, TMessage>> logger;
        readonly Dictionary<TKey, SubscriptionHolder> subscriptions;
        readonly object gate;

        public ImmutableArrayMessageBroker(ILogger<ImmutableArrayMessageBroker<TKey, TMessage>> logger)
        {
            this.logger = logger;
            this.subscriptions = new Dictionary<TKey, SubscriptionHolder>();
            this.gate = new object();
        }

        public void Publish(TKey id, TMessage message)
        {
            SubscriptionHolder holder;
            lock (gate)
            {
                if (!subscriptions.TryGetValue(id, out holder!))
                {
                    return;
                }
            }

            // outside from lock.
            var array = holder.GetHandlers();
            for (int i = 0; i < array.Length; i++)
            {
                array[i].Item2.Handle(message);
            }
        }

        public IDisposable Subscribe(TKey id, IMessageHandler<TMessage> handler)
        {
            lock (gate)
            {
                if (!subscriptions.TryGetValue(id, out var holder))
                {
                    subscriptions[id] = holder = new SubscriptionHolder();
                }

                return holder.RegisterHandler(this, id, handler);
            }
        }

        sealed class SubscriptionHolder
        {
            (IDisposable, IMessageHandler<TMessage>)[] handlers;

            public SubscriptionHolder()
            {
                handlers = Array.Empty<(IDisposable, IMessageHandler<TMessage>)>();
            }

            public (IDisposable, IMessageHandler<TMessage>)[] GetHandlers()
            {
                return handlers;
            }

            public IDisposable RegisterHandler(ImmutableArrayMessageBroker<TKey, TMessage> core, TKey TKey, IMessageHandler<TMessage> handler)
            {
                lock (core.gate)
                {
                    var subscription = new Subscription(core, TKey, this);

                    var newArray = new (IDisposable, IMessageHandler<TMessage>)[handlers.Length + 1];
                    Array.Copy(handlers, 0, newArray, 0, handlers.Length);
                    newArray[newArray.Length - 1] = (subscription, handler);
                    handlers = newArray; // replace new.

                    return subscription;
                }
            }

            sealed class Subscription : IDisposable
            {
                readonly ImmutableArrayMessageBroker<TKey, TMessage> core;
                readonly TKey id;
                readonly SubscriptionHolder holder;

                public Subscription(ImmutableArrayMessageBroker<TKey, TMessage> core, TKey id, SubscriptionHolder holder)
                {
                    this.core = core;
                    this.id = id;
                    this.holder = holder;
                }

                public void Dispose()
                {
                    lock (core.gate)
                    {
                        // TODO:remove lambda this capture.
                        holder.handlers = ArrayUtil.ImmutableRemove(holder.handlers, x => x.Item1 == this);
                        if (holder.handlers.Length == 0)
                        {
                            core.subscriptions.Remove(id);
                        }
                    }
                }
            }
        }
    }
}
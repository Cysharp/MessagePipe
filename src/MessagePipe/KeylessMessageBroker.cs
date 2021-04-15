using MessagePipe.Internal;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace MessagePipe
{
    public interface IMessageBroker<TMessage> : IPublisher<TMessage>, ISubscriber<TMessage>
    {
    }

    public sealed class MessageBroker<TMessage> : IPublisher<TMessage>, ISubscriber<TMessage>
    {
        readonly IMessageBroker<TMessage> core;

        public MessageBroker(IMessageBroker<TMessage> core)
        {
            this.core = core;
        }

        public void Publish(TMessage message)
        {
            core.Publish(message);
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler)
        {
            // TODO:filter?
            

            if (handler is IAttachedFilter attached)
            {

            }

            return core.Subscribe(handler);
        }

        static void RunCore(TMessage message)
        {
            
        }
    }

    public sealed class ConcurrentDictionaryMessageBroker<TMessage> : IMessageBroker<TMessage>
    {
        readonly ConcurrentDictionary<IDisposable, IMessageHandler<TMessage>> handlers;

        public ConcurrentDictionaryMessageBroker()
        {
            this.handlers = new ConcurrentDictionary<IDisposable, IMessageHandler<TMessage>>();
        }

        public void Publish(TMessage message)
        {
            foreach (var item in handlers)
            {
                item.Value.Handle(message);
            }
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler)
        {
            var subscription = new Subscription(this);
            handlers.TryAdd(subscription, handler);
            return subscription;
        }

        sealed class Subscription : IDisposable
        {
            readonly ConcurrentDictionaryMessageBroker<TMessage> core;

            public Subscription(ConcurrentDictionaryMessageBroker<TMessage> core)
            {
                this.core = core;
            }

            public void Dispose()
            {
                core.handlers.TryRemove(this, out _);
            }
        }
    }

    public sealed class ImmutableArrayMessageBroker<TMessage> : IMessageBroker<TMessage>
    {
        (IDisposable, IMessageHandler<TMessage>)[] handlers;
        readonly object gate;

        public ImmutableArrayMessageBroker()
        {
            this.handlers = Array.Empty<(IDisposable, IMessageHandler<TMessage>)>();
            this.gate = new object();
        }

        public void Publish(TMessage message)
        {
            for (int i = 0; i < handlers.Length; i++)
            {
                handlers[i].Item2.Handle(message);
            }
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler)
        {
            var subscription = new Subscription(this);
            // TODO:ImmutableInterlocked.
            lock (gate)
            {
                handlers = ArrayUtil.ImmutableAdd(handlers, (subscription, handler));
            }
            return subscription;
        }

        sealed class Subscription : IDisposable
        {
            readonly ImmutableArrayMessageBroker<TMessage> core;

            public Subscription(ImmutableArrayMessageBroker<TMessage> core)
            {
                this.core = core;
            }

            public void Dispose()
            {
                lock (core.gate)
                {
                    core.handlers = ArrayUtil.ImmutableRemove(core.handlers, x => x.Item1 == this);
                }
            }
        }
    }
}
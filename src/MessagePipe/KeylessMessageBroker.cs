using MessagePipe.Internal;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace MessagePipe
{
    public interface IMessageBroker<TMessage> : IPublisher<TMessage>, ISubscriber<TMessage>
    {
    }

    public sealed class MessageBroker<TMessage> : IMessageBroker<TMessage>
    {
        readonly IMessageBroker<TMessage> core;
        readonly MessagePipeOptions options;
        readonly IServiceProvider provider;

        public MessageBroker(IMessageBroker<TMessage> core, MessagePipeOptions options, IServiceProvider provider)
        {
            this.core = core;
            this.options = options;
            this.provider = provider;
        }

        public void Publish(TMessage message)
        {
            core.Publish(message);
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler)
        {
            var handlerFilters = FilterCache<MessageHandlerFilterAttribute, MessageHandlerFilter>.GetOrAddFilters(handler.GetType(), provider);
            var globalFilters = options.GetGlobalMessageHandlerFilters(provider);

            if (handlerFilters.Length != 0 || globalFilters.Length != 0)
            {
                handler = new FilterAttachedMessageHandler<TMessage>(handler, handlerFilters.Concat(globalFilters));
            }

            return core.Subscribe(handler);
        }
    }

    public sealed class ConcurrentDictionaryMessageBroker<TMessage> : IMessageBroker<TMessage>
    {
        readonly ConcurrentDictionary<IDisposable, IMessageHandler<TMessage>> handlers;
        readonly MessagePipeDiagnosticsInfo diagnotics;

        public ConcurrentDictionaryMessageBroker(MessagePipeDiagnosticsInfo diagnotics)
        {
            this.handlers = new ConcurrentDictionary<IDisposable, IMessageHandler<TMessage>>();
            this.diagnotics = diagnotics;
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
            diagnotics.IncrementSubscribe(subscription);
            return subscription;
        }

        sealed class Subscription : IDisposable
        {
            bool isDisposed;
            readonly ConcurrentDictionaryMessageBroker<TMessage> core;

            public Subscription(ConcurrentDictionaryMessageBroker<TMessage> core)
            {
                this.core = core;
            }

            public void Dispose()
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    core.handlers.TryRemove(this, out _);
                    core.diagnotics.DecrementSubscribe(this);
                }
            }
        }
    }

    public sealed class ImmutableArrayMessageBroker<TMessage> : IMessageBroker<TMessage>
    {
        (IDisposable, IMessageHandler<TMessage>)[] handlers;
        readonly MessagePipeDiagnosticsInfo diagnotics;
        readonly object gate;

        public ImmutableArrayMessageBroker(MessagePipeDiagnosticsInfo diagnotics)
        {
            this.handlers = Array.Empty<(IDisposable, IMessageHandler<TMessage>)>();
            this.gate = new object();
            this.diagnotics = diagnotics;
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
            lock (gate)
            {
                handlers = ArrayUtil.ImmutableAdd(handlers, (subscription, handler));
                diagnotics.IncrementSubscribe(subscription);
            }
            return subscription;
        }

        sealed class Subscription : IDisposable
        {
            bool isDisposed;
            readonly ImmutableArrayMessageBroker<TMessage> core;

            public Subscription(ImmutableArrayMessageBroker<TMessage> core)
            {
                this.core = core;
            }

            public void Dispose()
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    lock (core.gate)
                    {
                        core.handlers = ArrayUtil.ImmutableRemove(core.handlers, (x, state) => x.Item1 == state, this);
                        core.diagnotics.DecrementSubscribe(this);
                    }
                }
            }
        }
    }
}
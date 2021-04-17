using MessagePipe.Internal;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MessagePipe
{
    public sealed class MessageBroker<TMessage> : IPublisher<TMessage>, ISubscriber<TMessage>
    {
        readonly MessageBrokerCore<TMessage> core;
        readonly MessagePipeOptions options;
        readonly FilterCache<MessageHandlerFilterAttribute, MessageHandlerFilter> filterCache;
        readonly IServiceProvider provider;

        public MessageBroker(MessageBrokerCore<TMessage> core, MessagePipeOptions options, FilterCache<MessageHandlerFilterAttribute, MessageHandlerFilter> filterCache, IServiceProvider provider)
        {
            this.core = core;
            this.options = options;
            this.filterCache = filterCache;
            this.provider = provider;
        }

        public void Publish(TMessage message)
        {
            core.Publish(message);
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter[] filters)
        {
            var handlerFilters = filterCache.GetOrAddFilters(handler.GetType(), provider);
            var globalFilters = options.GetGlobalMessageHandlerFilters(provider);

            if (filters.Length != 0 || handlerFilters.Length != 0 || globalFilters.Length != 0)
            {
                handler = new FilterAttachedMessageHandler<TMessage>(handler, ArrayUtil.Concat(filters, handlerFilters, globalFilters));
            }

            return core.Subscribe(handler);
        }
    }

    public sealed class MessageBrokerCore<TMessage>
    {
        readonly FreeList<IDisposable, IMessageHandler<TMessage>> handlers;
        readonly MessagePipeDiagnosticsInfo diagnotics;
        readonly object gate;

        public MessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics)
        {
            this.handlers = new FreeList<IDisposable, IMessageHandler<TMessage>>();
            this.diagnotics = diagnotics;
            this.gate = new object();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Publish(TMessage message)
        {
            var array = handlers.GetUnsafeRawItems();
            for (int i = 0; i < array.Length; i++)
            {
                array[i]?.Handle(message);
            }
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler)
        {
            var subscription = new Subscription(this);
            lock (gate)
            {
                handlers.Add(subscription, handler);
            }
            diagnotics.IncrementSubscribe(subscription);
            return subscription;
        }

        sealed class Subscription : IDisposable
        {
            bool isDisposed;
            readonly MessageBrokerCore<TMessage> core;

            public Subscription(MessageBrokerCore<TMessage> core)
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
                        core.handlers.Remove(this);
                    }
                    core.diagnotics.DecrementSubscribe(this);
                }
            }
        }
    }
}
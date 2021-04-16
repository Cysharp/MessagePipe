using MessagePipe.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MessagePipe
{
    public interface IMessageBroker<TMessage> : IPublisher<TMessage>, ISubscriber<TMessage>
    {
    }

    public sealed class MessageBroker<TMessage> : IPublisher<TMessage>, ISubscriber<TMessage>
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
            List<(MessageHandlerFilter, int)>? list = null;

            foreach (var item in options.GetRequestHandler(provider))
            {
                if (list == null) list = new List<(MessageHandlerFilter, int)>();
                list.Add(item);
            }

            if (handler is IAttachedFilter attached)
            {
                foreach (var item in attached.Filters)
                {
                    var filterAttr = (MessagePipeFilterAttribute)item;
                    if (!typeof(MessageHandlerFilter).IsAssignableFrom(filterAttr.Type))
                    {
                        // TODO:error msg;
                        throw new Exception();
                    }

                    var t = (MessageHandlerFilter)provider.GetRequiredService(filterAttr.Type);

                    if (list == null) list = new List<(MessageHandlerFilter, int)>();
                    list.Add((t, filterAttr.Order));
                }
            }

            // TODO:use filter cache
            var filterAttributes = handler.GetType().GetCustomAttributes(typeof(MessagePipeFilterAttribute), true);
            foreach (var item in filterAttributes)
            {
                var filterAttr = (MessagePipeFilterAttribute)item;
                if (!typeof(MessageHandlerFilter).IsAssignableFrom(filterAttr.Type))
                {
                    // TODO:error msg;
                    throw new Exception();
                }

                var t = (MessageHandlerFilter)provider.GetRequiredService(filterAttr.Type);

                if (list == null) list = new List<(MessageHandlerFilter, int)>();
                list.Add((t, filterAttr.Order));
            }

            if (list != null)
            {
                handler = new FilterAttachedMessageHandler<TMessage>(handler, list);
            }

            return core.Subscribe(handler);
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
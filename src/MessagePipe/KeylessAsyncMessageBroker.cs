using MessageBroker.Internal;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    public interface IAsyncMessageBroker<TMessage> : IAsyncPublisher<TMessage>, IAsyncSubscriber<TMessage>
    {
    }

    public sealed class AsyncMessageBroker<TMessage> : IAsyncPublisher<TMessage>, IAsyncSubscriber<TMessage>
    {
        readonly IAsyncMessageBroker<TMessage> core;

        public AsyncMessageBroker(IAsyncMessageBroker<TMessage> core)
        {
            this.core = core;
        }

        public ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken)
        {
            return core.PublishAsync(message, cancellationToken);
        }

        public IDisposable Subscribe(IAsyncMessageHandler<TMessage> handler)
        {
            return core.Subscribe(handler);
        }
    }

    internal sealed class ConcurrentDictionaryAsyncMessageBroker<TMessage> : IAsyncMessageBroker<TMessage>
    {
        readonly ConcurrentDictionary<IDisposable, IAsyncMessageHandler<TMessage>> handlers;

        public ConcurrentDictionaryAsyncMessageBroker()
        {
            this.handlers = new ConcurrentDictionary<IDisposable, IAsyncMessageHandler<TMessage>>();
        }

        public async ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken)
        {
            foreach (var item in handlers)
            {
                // TODO:await mode(parallel? sequential?)
                await item.Value.HandleAsync(message, cancellationToken);
            }
        }

        public IDisposable Subscribe(IAsyncMessageHandler<TMessage> handler)
        {
            var subscription = new Subscription(this);
            handlers.TryAdd(subscription, handler);
            return subscription;
        }

        sealed class Subscription : IDisposable
        {
            readonly ConcurrentDictionaryAsyncMessageBroker<TMessage> core;

            public Subscription(ConcurrentDictionaryAsyncMessageBroker<TMessage> core)
            {
                this.core = core;
            }

            public void Dispose()
            {
                core.handlers.TryRemove(this, out _);
            }
        }
    }

    internal sealed class ImmutableArrayAsyncMessageBroker<TMessage> : IAsyncMessageBroker<TMessage>
    {
        (IDisposable, IAsyncMessageHandler<TMessage>)[] handlers;
        readonly object gate;

        public ImmutableArrayAsyncMessageBroker()
        {
            this.handlers = Array.Empty<(IDisposable, IAsyncMessageHandler<TMessage>)>();
            this.gate = new object();
        }

        public async ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken)
        {
            for (int i = 0; i < handlers.Length; i++)
            {
                // TODO:await strategy
                await handlers[i].Item2.HandleAsync(message, cancellationToken);
            }
        }

        public IDisposable Subscribe(IAsyncMessageHandler<TMessage> handler)
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
            readonly ImmutableArrayAsyncMessageBroker<TMessage> core;

            public Subscription(ImmutableArrayAsyncMessageBroker<TMessage> core)
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
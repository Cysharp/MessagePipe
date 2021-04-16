using MessagePipe.Internal;
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

        public void Publish(TMessage message, CancellationToken cancellationToken)
        {
            core.Publish(message, cancellationToken);
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

    public sealed class ConcurrentDictionaryAsyncMessageBroker<TMessage> : IAsyncMessageBroker<TMessage>
    {
        readonly ConcurrentDictionary<IDisposable, IAsyncMessageHandler<TMessage>> handlers;

        public ConcurrentDictionaryAsyncMessageBroker()
        {
            this.handlers = new ConcurrentDictionary<IDisposable, IAsyncMessageHandler<TMessage>>();
        }

        public void Publish(TMessage message, CancellationToken cancellationToken)
        {
            foreach (var item in handlers)
            {
                item.Value.HandleAsync(message, cancellationToken).Forget();
            }
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

    public sealed class ImmutableArrayAsyncMessageBroker<TMessage> : IAsyncMessageBroker<TMessage>
    {
        (IDisposable, IAsyncMessageHandler<TMessage>)[] handlers;
        readonly object gate;

        public ImmutableArrayAsyncMessageBroker()
        {
            this.handlers = Array.Empty<(IDisposable, IAsyncMessageHandler<TMessage>)>();
            this.gate = new object();
        }

        public void Publish(TMessage message, CancellationToken cancellationToken)
        {
            // require to get current field to local.
            var array = Volatile.Read(ref handlers);
            for (int i = 0; i < array.Length; i++)
            {
                array[i].Item2.HandleAsync(message, cancellationToken).Forget();
            }
        }

        public async ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken)
        {
            var array = Volatile.Read(ref handlers);
            for (int i = 0; i < array.Length; i++)
            {
                // TODO:await strategy
                await array[i].Item2.HandleAsync(message, cancellationToken);
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
                    core.handlers = ArrayUtil.ImmutableRemove(core.handlers, (x, state) => x.Item1 == state, this);
                }
            }
        }
    }
}
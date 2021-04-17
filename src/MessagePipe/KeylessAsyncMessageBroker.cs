using MessagePipe.Internal;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    public sealed class AsyncMessageBroker<TMessage> : IAsyncPublisher<TMessage>, IAsyncSubscriber<TMessage>
    {
        readonly AsyncMessageBrokerCore<TMessage> core;
        readonly MessagePipeOptions options;
        readonly FilterCache<AsyncMessageHandlerFilterAttribute, AsyncMessageHandlerFilter> filterCache;
        readonly IServiceProvider provider;

        public AsyncMessageBroker(AsyncMessageBrokerCore<TMessage> core, MessagePipeOptions options, FilterCache<AsyncMessageHandlerFilterAttribute, AsyncMessageHandlerFilter> filterCache, IServiceProvider provider)
        {
            this.core = core;
            this.options = options;
            this.filterCache = filterCache;
            this.provider = provider;
        }

        public void Publish(TMessage message, CancellationToken cancellationToken)
        {
            core.Publish(message, cancellationToken);
        }

        public ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken)
        {
            return core.PublishAsync(message, options.DefaultAsyncPublishStrategy, cancellationToken);
        }

        public ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken)
        {
            return core.PublishAsync(message, publishStrategy, cancellationToken);
        }

        public IDisposable Subscribe(IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter[] filters)
        {
            var handlerFilters = filterCache.GetOrAddFilters(handler.GetType(), provider);
            var globalFilters = options.GetGlobalAsyncMessageHandlerFilters(provider);

            if (filters.Length != 0 || handlerFilters.Length != 0 || globalFilters.Length != 0)
            {
                handler = new FilterAttachedAsyncMessageHandler<TMessage>(handler, ArrayUtil.Concat(filters, handlerFilters, globalFilters));
            }

            return core.Subscribe(handler);
        }
    }

    public sealed class AsyncMessageBrokerCore<TMessage>
    {
        FreeList<IDisposable, IAsyncMessageHandler<TMessage>> handlers;
        readonly MessagePipeDiagnosticsInfo diagnotics;
        readonly object gate;

        public AsyncMessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics)
        {
            this.handlers = new FreeList<IDisposable, IAsyncMessageHandler<TMessage>>();
            this.diagnotics = diagnotics;
            this.gate = new object();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Publish(TMessage message, CancellationToken cancellationToken)
        {
            var array = handlers.GetUnsafeRawItems();
            for (int i = 0; i < array.Length; i++)
            {
                array[i]?.HandleAsync(message, cancellationToken).Forget();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default)
        {
            var array = handlers.GetUnsafeRawItems();
            if (publishStrategy == AsyncPublishStrategy.Sequential)
            {
                foreach (var item in array)
                {
                    if (item != null)
                    {
                        await item.HandleAsync(message, cancellationToken);
                    }
                }
            }
            else
            {
                await new AsyncHandlerWhenAll<TMessage>(array, message, cancellationToken);
            }
        }

        public IDisposable Subscribe(IAsyncMessageHandler<TMessage> handler)
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
            readonly AsyncMessageBrokerCore<TMessage> core;

            public Subscription(AsyncMessageBrokerCore<TMessage> core)
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
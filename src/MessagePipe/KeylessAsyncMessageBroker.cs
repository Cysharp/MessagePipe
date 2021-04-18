using MessagePipe.Internal;
using System;
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

    public sealed class AsyncMessageBrokerCore<TMessage> : IDisposable, IHandlerHolderMarker
    {
        FreeList<IAsyncMessageHandler<TMessage>> handlers;
        readonly MessagePipeDiagnosticsInfo diagnotics;
        readonly object gate = new object();
        bool isDisposed;

        public AsyncMessageBrokerCore(MessagePipeDiagnosticsInfo diagnotics)
        {
            this.handlers = new FreeList<IAsyncMessageHandler<TMessage>>();
            this.diagnotics = diagnotics;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Publish(TMessage message, CancellationToken cancellationToken)
        {
            var array = handlers.GetValues();
            for (int i = 0; i < array.Length; i++)
            {
                array[i]?.HandleAsync(message, cancellationToken).Forget();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default)
        {
            var array = handlers.GetValues();
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
            lock (gate)
            {
                if (isDisposed) return DisposableBag.Empty;

                var subscriptionKey = handlers.Add(handler);
                var subscription = new Subscription(this, subscriptionKey);
                diagnotics.IncrementSubscribe(this, subscription);
                return subscription;
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                // Dispose is called when scope is finished.
                if (!isDisposed && handlers.TryDispose(out var count))
                {
                    isDisposed = true;
                    diagnotics.RemoveTargetDiagnostics(this, count);
                }
            }
        }

        sealed class Subscription : IDisposable
        {
            bool isDisposed;
            readonly AsyncMessageBrokerCore<TMessage> core;
            readonly int subscriptionKey;

            public Subscription(AsyncMessageBrokerCore<TMessage> core, int subscriptionKey)
            {
                this.core = core;
                this.subscriptionKey = subscriptionKey;
            }

            public void Dispose()
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    lock (core.gate)
                    {
                        core.handlers.Remove(subscriptionKey, true);
                        core.diagnotics.DecrementSubscribe(core, this);
                    }
                }
            }
        }
    }
}
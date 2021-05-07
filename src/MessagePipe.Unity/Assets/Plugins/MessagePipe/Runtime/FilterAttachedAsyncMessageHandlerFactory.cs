using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    // Async

    [Preserve]
    public sealed class FilterAttachedAsyncMessageHandlerFactory
    {
        readonly MessagePipeOptions options;
        readonly AttributeFilterProvider<AsyncMessageHandlerFilterAttribute> filterProvider;
        readonly IServiceProvider provider;

        [Preserve]
        public FilterAttachedAsyncMessageHandlerFactory(MessagePipeOptions options, AttributeFilterProvider<AsyncMessageHandlerFilterAttribute> filterProvider, IServiceProvider provider)
        {
            this.options = options;
            this.filterProvider = filterProvider;
            this.provider = provider;
        }

        public IAsyncMessageHandler<TMessage> CreateAsyncMessageHandler<TMessage>(IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            var (globalLength, globalFilters) = options.GetGlobalAsyncMessageHandlerFilters(provider, typeof(TMessage));
            var (handlerLength, handlerFilters) = filterProvider.GetAttributeFilters(handler.GetType(), provider);

            if (filters.Length != 0 || globalLength != 0 || handlerLength != 0)
            {
                handler = new FilterAttachedAsyncMessageHandler<TMessage>(handler, globalFilters.Concat(handlerFilters).Concat(filters).Cast<AsyncMessageHandlerFilter<TMessage>>());
            }

            return handler;
        }
    }

    internal sealed class FilterAttachedAsyncMessageHandler<T> : IAsyncMessageHandler<T>
    {
        Func<T, CancellationToken, UniTask> handler;

        public FilterAttachedAsyncMessageHandler(IAsyncMessageHandler<T> body, IEnumerable<AsyncMessageHandlerFilter<T>> filters)
        {
            Func<T, CancellationToken, UniTask> next = body.HandleAsync;
            foreach (var f in filters.OrderByDescending(x => x.Order))
            {
                next = new AsyncMessageHandlerFilterRunner<T>(f, next).GetDelegate();
            }

            this.handler = next;
        }

        public UniTask HandleAsync(T message, CancellationToken cancellationToken)
        {
            return handler.Invoke(message, cancellationToken);
        }
    }

    internal sealed class AsyncMessageHandlerFilterRunner<T>
    {
        readonly AsyncMessageHandlerFilter<T> filter;
        readonly Func<T, CancellationToken, UniTask> next;

        public AsyncMessageHandlerFilterRunner(AsyncMessageHandlerFilter<T> filter, Func<T, CancellationToken, UniTask> next)
        {
            this.filter = filter;
            this.next = next;
        }

        public Func<T, CancellationToken, UniTask> GetDelegate() => HandleAsync;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        UniTask HandleAsync(T message, CancellationToken cancellationToken)
        {
            return filter.HandleAsync(message, cancellationToken, next);
        }
    }
}
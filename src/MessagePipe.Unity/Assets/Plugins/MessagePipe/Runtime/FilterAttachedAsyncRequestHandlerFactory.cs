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
    public sealed class FilterAttachedAsyncRequestHandlerFactory
    {
        readonly MessagePipeOptions options;
        readonly AttributeFilterProvider<AsyncRequestHandlerFilterAttribute> filterProvider;
        readonly IServiceProvider provider;

        [Preserve]
        public FilterAttachedAsyncRequestHandlerFactory(MessagePipeOptions options, AttributeFilterProvider<AsyncRequestHandlerFilterAttribute> filterProvider, IServiceProvider provider)
        {
            this.options = options;
            this.filterProvider = filterProvider;
            this.provider = provider;
        }

        public IAsyncRequestHandlerCore<TRequest, TResponse> CreateAsyncRequestHandler<TRequest, TResponse>(IAsyncRequestHandlerCore<TRequest, TResponse> handler)
        {
            var (globalLength, globalFilters) = options.GetGlobalAsyncRequestHandlerFilters(provider, typeof(TRequest), typeof(TResponse));
            var (handlerLength, handlerFilters) = filterProvider.GetAttributeFilters(handler.GetType(), provider);

            if (globalLength != 0 || handlerLength != 0)
            {
                handler = new FilterAttachedAsyncRequestHandler<TRequest, TResponse>(handler, globalFilters.Concat(handlerFilters).Cast<AsyncRequestHandlerFilter<TRequest, TResponse>>());
            }

            return handler;
        }
    }


    internal sealed class FilterAttachedAsyncRequestHandler<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse>
    {
        Func<TRequest, CancellationToken, UniTask<TResponse>> handler;

        public FilterAttachedAsyncRequestHandler(IAsyncRequestHandlerCore<TRequest, TResponse> body, IEnumerable<AsyncRequestHandlerFilter<TRequest, TResponse>> filters)
        {
            Func<TRequest, CancellationToken, UniTask<TResponse>> next = body.InvokeAsync;
            foreach (var f in filters.OrderByDescending(x => x.Order))
            {
                next = new AsyncRequestHandlerFilterRunner<TRequest, TResponse>(f, next).GetDelegate();
            }

            this.handler = next;
        }

        public UniTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }

    internal sealed class AsyncRequestHandlerFilterRunner<TRequest, TResponse>
    {
        readonly AsyncRequestHandlerFilter<TRequest, TResponse> filter;
        readonly Func<TRequest, CancellationToken, UniTask<TResponse>> next;

        public AsyncRequestHandlerFilterRunner(AsyncRequestHandlerFilter<TRequest, TResponse> filter, Func<TRequest, CancellationToken, UniTask<TResponse>> next)
        {
            this.filter = filter;
            this.next = next;
        }

        public Func<TRequest, CancellationToken, UniTask<TResponse>> GetDelegate() => InvokeAsync;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        UniTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken)
        {
            return filter.InvokeAsync(request, cancellationToken, next);
        }
    }
}
using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    // async

    public sealed class AsyncRequestHandler<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse>
    {
        Func<TRequest, CancellationToken, ValueTask<TResponse>> handler;

        public AsyncRequestHandler(IAsyncRequestHandlerCore<TRequest, TResponse> handler, MessagePipeOptions options, FilterCache<AsyncRequestHandlerFilterAttribute, AsyncRequestHandlerFilter> filterCache, IServiceProvider provider)
        {
            var handlerFilters = filterCache.GetOrAddFilters(handler.GetType(), provider);
            var globalFilters = options.GetGlobalAsyncRequestHandlerFilters(provider);

            Func<TRequest, CancellationToken, ValueTask<TResponse>> next = handler.InvokeAsync;
            if (handlerFilters.Length != 0 || globalFilters.Length != 0)
            {
                foreach (var f in ArrayUtil.Concat(handlerFilters, globalFilters).OrderByDescending(x => x.Order))
                {
                    next = new AsyncRequestHandlerFilterRunner<TRequest, TResponse>(f, next).GetDelegate();
                }
            }

            this.handler = next;
        }

        public ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return handler(request, cancellationToken);
        }
    }

    public sealed class AsyncRequestAllHandler<TRequest, TResponse> : IAsyncRequestAllHandler<TRequest, TResponse>
    {
        readonly IAsyncRequestHandlerCore<TRequest, TResponse>[] handlers;
        readonly AsyncPublishStrategy defaultAsyncPublishStrategy;

        public AsyncRequestAllHandler(IEnumerable<IAsyncRequestHandlerCore<TRequest, TResponse>> handlers, MessagePipeOptions options)
        {
            this.handlers = handlers.ToArray();
            this.defaultAsyncPublishStrategy = options.DefaultAsyncPublishStrategy;
        }

        public ValueTask<TResponse[]> InvokeAllAsync(TRequest request, CancellationToken cancellationToken)
        {
            return InvokeAllAsync(request, defaultAsyncPublishStrategy, cancellationToken);
        }

        public async ValueTask<TResponse[]> InvokeAllAsync(TRequest request, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken)
        {
            var responses = new TResponse[handlers.Length];

            if (publishStrategy == AsyncPublishStrategy.Sequential)
            {
                for (int i = 0; i < handlers.Length; i++)
                {
                    responses[i] = await handlers[i].InvokeAsync(request, cancellationToken);
                }
                return responses;
            }
            else
            {
                return await new AsyncRequestHandlerWhenAll<TRequest, TResponse>(handlers, request, cancellationToken);
            }
        }

        public async IAsyncEnumerable<TResponse> InvokeAllLazyAsync(TRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < handlers.Length; i++)
            {
                yield return await handlers[i].InvokeAsync(request);
            }
        }
    }
}

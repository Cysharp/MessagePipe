using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    // Sync

    public interface IRequestHandler
    {
    }

    public interface IRequestHandlerCore<in TRequest, out TResponse> : IRequestHandler
    {
        TResponse Invoke(TRequest request);
    }

    public interface IRequestHandler<in TRequest, out TResponse> : IRequestHandlerCore<TRequest, TResponse>
    {
    }

    public interface IRequestAllHandler<in TRequest, out TResponse>
    {
        TResponse[] InvokeAll(TRequest request);
        IEnumerable<TResponse> InvokeAllLazy(TRequest request);
    }

    public sealed class RequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    {
        Func<TRequest, TResponse> handler;

        public RequestHandler(IRequestHandlerCore<TRequest, TResponse> handler, MessagePipeOptions options, FilterCache<RequestHandlerFilterAttribute, RequestHandlerFilter> filterCache, IServiceProvider provider)
        {
            var handlerFilters = filterCache.GetOrAddFilters(handler.GetType(), provider);
            var globalFilters = options.GetGlobalRequestHandlerFilters(provider);

            Func<TRequest, TResponse> next = handler.Invoke;
            if (handlerFilters.Length != 0 || globalFilters.Length != 0)
            {
                foreach (var f in ArrayUtil.Concat(handlerFilters, globalFilters).OrderByDescending(x => x.Order))
                {
                    next = new RequestHandlerFilterRunner<TRequest, TResponse>(f, next).GetDelegate();
                }
            }

            this.handler = next;
        }

        public TResponse Invoke(TRequest request)
        {
            return handler.Invoke(request);
        }
    }

    public sealed class RequestAllHandler<TRequest, TResponse> : IRequestAllHandler<TRequest, TResponse>
    {
        readonly IRequestHandler<TRequest, TResponse>[] handlers;

        public RequestAllHandler(IEnumerable<IRequestHandler<TRequest, TResponse>> handlers)
        {
            this.handlers = handlers.ToArray();
        }

        public TResponse[] InvokeAll(TRequest request)
        {
            var responses = new TResponse[handlers.Length];

            for (int i = 0; i < handlers.Length; i++)
            {
                responses[i] = handlers[i].Invoke(request);
            }

            return responses;
        }

        public IEnumerable<TResponse> InvokeAllLazy(TRequest request)
        {
            for (int i = 0; i < handlers.Length; i++)
            {
                yield return handlers[i].Invoke(request);
            }
        }
    }

    // Async

    public interface IAsyncRequestHandler
    {
    }

    public interface IAsyncRequestHandlerCore<in TRequest, TResponse> : IAsyncRequestHandler
    {
        ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default);
    }

    public interface IAsyncRequestHandler<in TRequest, TResponse> : IAsyncRequestHandlerCore<TRequest, TResponse>
    {
    }

    public interface IAsyncRequestAllHandler<in TRequest, TResponse>
    {
        ValueTask<TResponse[]> InvokeAllAsync(TRequest request, CancellationToken cancellationToken = default);
        ValueTask<TResponse[]> InvokeAllAsync(TRequest request, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default);
        IAsyncEnumerable<TResponse> InvokeAllLazyAsync(TRequest request, CancellationToken cancellationToken = default);
    }

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
        readonly IAsyncRequestHandler<TRequest, TResponse>[] handlers;
        readonly AsyncPublishStrategy defaultAsyncPublishStrategy;

        public AsyncRequestAllHandler(IEnumerable<IAsyncRequestHandler<TRequest, TResponse>> handlers, MessagePipeOptions options)
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

using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    // async

    public sealed class AsyncRequestHandler<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse>
    {
        Func<TRequest, CancellationToken, UniTask<TResponse>> handler;

        public AsyncRequestHandler(IAsyncRequestHandlerCore<TRequest, TResponse> handler, MessagePipeOptions options, FilterCache<AsyncRequestHandlerFilterAttribute, AsyncRequestHandlerFilter> filterCache, IServiceProvider provider)
        {
            var handlerFilters = filterCache.GetOrAddFilters(handler.GetType(), provider);
            var globalFilters = options.GetGlobalAsyncRequestHandlerFilters(provider);

            Func<TRequest, CancellationToken, UniTask<TResponse>> next = handler.InvokeAsync;
            if (handlerFilters.Length != 0 || globalFilters.Length != 0)
            {
                foreach (var f in ArrayUtil.Concat(handlerFilters, globalFilters).OrderByDescending(x => x.Order))
                {
                    next = new AsyncRequestHandlerFilterRunner<TRequest, TResponse>(f, next).GetDelegate();
                }
            }

            this.handler = next;
        }

        public UniTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return handler(request, cancellationToken);
        }
    }

    public sealed class AsyncRequestAllHandler<TRequest, TResponse> : IAsyncRequestAllHandler<TRequest, TResponse>
    {
        Func<TRequest, CancellationToken, UniTask<TResponse>>[] handlers;
        readonly AsyncPublishStrategy defaultAsyncPublishStrategy;

        public AsyncRequestAllHandler(IEnumerable<IAsyncRequestHandlerCore<TRequest, TResponse>> handlers, MessagePipeOptions options, FilterCache<AsyncRequestHandlerFilterAttribute, AsyncRequestHandlerFilter> filterCache, IServiceProvider provider)
        {
            var globalFilters = options.GetGlobalAsyncRequestHandlerFilters(provider);

            this.handlers = new Func<TRequest, CancellationToken, UniTask<TResponse>>[handlers.Count()];
            int currentIndex = 0;
            foreach (var handler in handlers)
            {
                var handlerFilters = filterCache.GetOrAddFilters(handler.GetType(), provider);

                Func<TRequest, CancellationToken, UniTask<TResponse>> next = handler.InvokeAsync;
                if (handlerFilters.Length != 0 || globalFilters.Length != 0)
                {
                    foreach (var f in ArrayUtil.Concat(handlerFilters, globalFilters).OrderByDescending(x => x.Order))
                    {
                        next = new AsyncRequestHandlerFilterRunner<TRequest, TResponse>(f, next).GetDelegate();
                    }
                }
                this.handlers[currentIndex++] = next;
            }
        }

        public UniTask<TResponse[]> InvokeAllAsync(TRequest request, CancellationToken cancellationToken)
        {
            return InvokeAllAsync(request, defaultAsyncPublishStrategy, cancellationToken);
        }

        public async UniTask<TResponse[]> InvokeAllAsync(TRequest request, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken)
        {
            var responses = new TResponse[handlers.Length];

            if (publishStrategy == AsyncPublishStrategy.Sequential)
            {
                for (int i = 0; i < handlers.Length; i++)
                {
                    responses[i] = await handlers[i].Invoke(request, cancellationToken);
                }
                return responses;
            }
            else
            {
                return await new AsyncRequestHandlerWhenAll<TRequest, TResponse>(handlers, request, cancellationToken);
            }
        }

#if UNITY_2018_3_OR_NEWER

        public Cysharp.Threading.Tasks.IUniTaskAsyncEnumerable<TResponse> InvokeAllLazyAsync(TRequest request, CancellationToken cancellationToken)
        {

           return Cysharp.Threading.Tasks.Linq.UniTaskAsyncEnumerable.Create<TResponse>(async (writer, token) =>
           {
               for (int i = 0; i < handlers.Length; i++)
               {
                   await writer.YieldAsync(await handlers[i].InvokeAsync(request, cancellationToken));
               }
           });
        }
#else

        public async IUniTaskAsyncEnumerable<TResponse> InvokeAllLazyAsync(TRequest request,  CancellationToken cancellationToken)
        {
            for (int i = 0; i < handlers.Length; i++)
            {
                yield return await handlers[i].Invoke(request, cancellationToken);
            }
        }

#endif
    }
}
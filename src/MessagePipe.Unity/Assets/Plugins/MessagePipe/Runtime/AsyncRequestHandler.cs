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

    [Preserve]
    public sealed class AsyncRequestHandler<TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse>
    {
        readonly IAsyncRequestHandlerCore<TRequest, TResponse> handler;

        [Preserve]
        public AsyncRequestHandler(IAsyncRequestHandlerCore<TRequest, TResponse> handler, FilterAttachedAsyncRequestHandlerFactory handlerFactory)
        {
            this.handler = handlerFactory.CreateAsyncRequestHandler<TRequest, TResponse>(handler);
        }

        public UniTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return handler.InvokeAsync(request, cancellationToken);
        }
    }

    [Preserve]
    public sealed class AsyncRequestAllHandler<TRequest, TResponse> : IAsyncRequestAllHandler<TRequest, TResponse>
    {
        readonly IAsyncRequestHandlerCore<TRequest, TResponse>[] handlers;
        readonly AsyncPublishStrategy defaultAsyncPublishStrategy;

        [Preserve]
        public AsyncRequestAllHandler(IEnumerable<IAsyncRequestHandlerCore<TRequest, TResponse>> handlers, FilterAttachedAsyncRequestHandlerFactory handlerFactory, MessagePipeOptions options)
        {
            var collection = (handlers as ICollection<IAsyncRequestHandlerCore<TRequest, TResponse>>) ?? handlers.ToArray();

            var array = new IAsyncRequestHandlerCore<TRequest, TResponse>[collection.Count];
            var i = 0;
            foreach (var item in collection)
            {
                array[i++] = handlerFactory.CreateAsyncRequestHandler(item);
            }

            this.handlers = array;
            this.defaultAsyncPublishStrategy = options.DefaultAsyncPublishStrategy;
        }

        public UniTask<TResponse[]> InvokeAllAsync(TRequest request, CancellationToken cancellationToken)
        {
            return InvokeAllAsync(request, defaultAsyncPublishStrategy, cancellationToken);
        }

        public async UniTask<TResponse[]> InvokeAllAsync(TRequest request, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken)
        {
            if (publishStrategy == AsyncPublishStrategy.Sequential)
            {
                var responses = new TResponse[handlers.Length];
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
                yield return await handlers[i].InvokeAsync(request, cancellationToken);
            }
        }

#endif
    }
}
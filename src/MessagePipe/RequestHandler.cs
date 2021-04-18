using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
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
}

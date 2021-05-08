using MessagePipe.Internal;
using System.Collections.Generic;
using System.Linq;

namespace MessagePipe
{
    [Preserve]
    public sealed class RequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    {
        readonly IRequestHandlerCore<TRequest, TResponse> handler;

        [Preserve]
        public RequestHandler(IRequestHandlerCore<TRequest, TResponse> handler, FilterAttachedRequestHandlerFactory handlerFactory)
        {
            this.handler = handlerFactory.CreateRequestHandler<TRequest, TResponse>(handler);
        }

        public TResponse Invoke(TRequest request)
        {
            return handler.Invoke(request);
        }
    }

    [Preserve]
    public sealed class RequestAllHandler<TRequest, TResponse> : IRequestAllHandler<TRequest, TResponse>
    {
        readonly IRequestHandlerCore<TRequest, TResponse>[] handlers;

        [Preserve]
        public RequestAllHandler(IEnumerable<IRequestHandlerCore<TRequest, TResponse>> handlers, FilterAttachedRequestHandlerFactory handlerFactory)
        {
            var collection = (handlers as ICollection<IRequestHandlerCore<TRequest, TResponse>>) ?? handlers.ToArray();

            var array = new IRequestHandlerCore<TRequest, TResponse>[collection.Count];
            var i = 0;
            foreach (var item in collection)
            {
                array[i++] = handlerFactory.CreateRequestHandler(item);
            }

            this.handlers = array;
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
using System.Collections.Generic;
using System.Linq;

namespace MessagePipe
{
    public interface IRequestHandler<in TRequest, out TResponse>
    {
        TResponse Execute(TRequest request);
    }

    public interface IRequestAllHandler<in TRequest, out TResponse>
    {
        TResponse[] ExecuteAll(TRequest request);
        IEnumerable<TResponse> ExecuteAllLazy(TRequest request);
    }

    // TODO:AsyncRequest?

    public sealed class RequestAllHandler<TRequest, TResponse> : IRequestAllHandler<TRequest, TResponse>
    {
        readonly IRequestHandler<TRequest, TResponse>[] handlers;

        public RequestAllHandler(IEnumerable<IRequestHandler<TRequest, TResponse>> handlers)
        {
            this.handlers = handlers.ToArray();
        }

        public TResponse[] ExecuteAll(TRequest request)
        {
            var responses = new TResponse[handlers.Length];

            for (int i = 0; i < handlers.Length; i++)
            {
                responses[i] = handlers[i].Execute(request);
            }

            return responses;
        }

        public IEnumerable<TResponse> ExecuteAllLazy(TRequest request)
        {
            for (int i = 0; i < handlers.Length; i++)
            {
                yield return handlers[i].Execute(request);
            }
        }
    }
}

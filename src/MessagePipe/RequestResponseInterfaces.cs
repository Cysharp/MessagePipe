using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePipe
{
    public interface IRequestHandler<TKey, in TRequest, out TResponse>
    {
        TKey Key { get; }
        TResponse Execute(TKey key, TRequest request);
    }

    // TODO:rename keyed...???

    public interface IRequestAllHandler<TKey, in TRequest, out TResponse>
    {
        IEnumerable<TResponse> ExecuteAll(TKey key, TRequest request);
    }

    public interface IAsyncRequestHandler<TKey, in TRequest, TResponse>
    {
        ValueTask<TResponse> ExecuteAsync(TKey key, TRequest request);
    }

    public interface IAsyncRequestAllHandler<TKey, in TRequest, out TResponse>
    {
        IAsyncEnumerable<TResponse> ExecuteAllAsync(TKey key, TRequest request);
    }

    // keyless


    public interface IRequestHandler<in TRequest, out TResponse>
    {
        TResponse Execute(TRequest request);
    }

    public interface IRequestAllHandler<in TRequest, out TResponse>
    {
        TResponse[] ExecuteAll(TRequest request);
    }



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
    }

    public class KeyedHandler<TKey, TRequest, TResponse> : IRequestHandler<TKey, TRequest, TResponse>
    {
        readonly IRequestHandler<TRequest, TResponse> handler;

        // TODO:msg
        public TKey Key => throw new NotSupportedException("TODO");

        public KeyedHandler(IRequestHandler<TKey, TRequest, TResponse> handler)
        {
            // this.handler = handler;
        }

        public TResponse Execute(TKey key, TRequest request)
        {


            throw new NotImplementedException();
        }


    }

}

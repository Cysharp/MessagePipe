using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

    // Remote

    public interface IRemoteRequestHandler<in TRequest, TResponse>
    // where TAsyncRequestHandler : IAsyncRequestHandler<TRequest, TResponse>
    {
        ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default);
    }

    public class RemoteRequestException : Exception
    {
        public RemoteRequestException(string message)
            : base(message)
        {
        }
    }

    // almostly internal usage for IRemoteRequestHandler type search
    public static class AsyncRequestHandlerRegistory
    {
        static ConcurrentDictionary<(string, string), Type> types = new ConcurrentDictionary<(string, string), Type>();

        public static void Add(Type handlerType)
        {
            foreach (var interfaceType in handlerType.GetInterfaces().Where(x => x.IsGenericType && x.Name.StartsWith("IAsyncRequestHandlerCore")))
            {
                var genArgs = interfaceType.GetGenericArguments();
                types[(genArgs[0].FullName!, genArgs[1].FullName)!] = handlerType;
            }
        }

        public static void Add(Type requestType, Type responseType, Type handlerType)
        {
            types[(requestType.FullName!, responseType.FullName!)] = handlerType;
        }

        public static Type Get(string requestType, string responseType)
        {
            if (types.TryGetValue((requestType, responseType), out var result))
            {
                return result;
            }
            throw new InvalidOperationException($"IAsyncHandler<{requestType}, {responseType}> is not registered.");
        }
    }
}

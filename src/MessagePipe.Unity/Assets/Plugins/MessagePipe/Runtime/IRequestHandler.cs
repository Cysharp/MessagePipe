using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

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
        UniTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default);
    }

    public interface IAsyncRequestHandler<in TRequest, TResponse> : IAsyncRequestHandlerCore<TRequest, TResponse>
    {
    }

    public interface IAsyncRequestAllHandler<in TRequest, TResponse>
    {
        UniTask<TResponse[]> InvokeAllAsync(TRequest request, CancellationToken cancellationToken = default);
        UniTask<TResponse[]> InvokeAllAsync(TRequest request, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default);
        IUniTaskAsyncEnumerable<TResponse> InvokeAllLazyAsync(TRequest request, CancellationToken cancellationToken = default);
    }


}

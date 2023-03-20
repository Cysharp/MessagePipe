using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe;

public interface IMessagePipeMediator
{
    IRequestHandler<TRequest, TResponse> GetHandler<TRequest, TResponse>();
    TResponse Send<TRequest, TResponse>(TRequest request);
    
    IAsyncRequestHandler<TRequest, TResponse> GetAsyncHandler<TRequest, TResponse>();
    ValueTask<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default);
}
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe;

public interface IMessagePipeMediator
{
    TResponse Send<TRequest, TResponse>(TRequest request);
    
    ValueTask<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default);
}
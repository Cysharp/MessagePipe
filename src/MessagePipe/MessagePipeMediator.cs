using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MessagePipe;

public class MessagePipeMediator : IMessagePipeMediator
{
    private readonly IServiceProvider _serviceProvider;
    
    
    public MessagePipeMediator(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
    }

    public TResponse Send<TRequest, TResponse>(TRequest request)
    {
        IRequestHandler<TRequest, TResponse> handler =
            _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        return handler.Invoke(request);
    }


    public async ValueTask<TResponse> SendAsync<TRequest, TResponse>(TRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IAsyncRequestHandler<TRequest, TResponse> handler =
            _serviceProvider.GetRequiredService<IAsyncRequestHandler<TRequest, TResponse>>();

        return await handler.InvokeAsync(request);
    }
    
}
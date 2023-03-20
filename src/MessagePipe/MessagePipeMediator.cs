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


    public IRequestHandler<TRequest, TResponse> GetHandler<TRequest, TResponse>()
    {
        return _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
    }
    
    public TResponse Send<TRequest, TResponse>(TRequest request)
    {
        var handler = this.GetHandler<TRequest, TResponse>();

        return handler.Invoke(request);
    }



    public IAsyncRequestHandler<TRequest, TResponse> GetAsyncHandler<TRequest, TResponse>()
    {
        return _serviceProvider.GetRequiredService<IAsyncRequestHandler<TRequest, TResponse>>();
    }

    public async ValueTask<TResponse> SendAsync<TRequest, TResponse>(TRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var asyncHandler = this.GetAsyncHandler<TRequest, TResponse>();

        return await asyncHandler.InvokeAsync(request);
    }
    
    
    
    
    
}
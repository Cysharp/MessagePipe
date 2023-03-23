using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MessagePipe;

public class MessagePipeMediator : IMessagePipeMediator
{
    private readonly ConcurrentDictionary<(Type, Type), IRequestHandler> syncHandlersCache = new();
    private readonly ConcurrentDictionary<(Type, Type), IAsyncRequestHandler> asyncHandlersCache = new();
    
    private readonly IServiceProvider _serviceProvider;
    
    
    public MessagePipeMediator(IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
    }

    public TResponse Send<TRequest, TResponse>(TRequest request)
    {
        var syncHandler = (IRequestHandler<TRequest, TResponse>)
            syncHandlersCache.GetOrAdd(
            (typeof(TRequest), typeof(TResponse)), 
            (a) => _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>());

        return syncHandler.Invoke(request);
    }


    public async ValueTask<TResponse> SendAsync<TRequest, TResponse>(TRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var asyncHandler = (IAsyncRequestHandler<TRequest, TResponse>)
            asyncHandlersCache.GetOrAdd(
            (typeof(TRequest), typeof(TResponse)), 
            (a) => _serviceProvider.GetRequiredService<IAsyncRequestHandler<TRequest, TResponse>>());

        return await asyncHandler.InvokeAsync(request, cancellationToken);
    }
    
}
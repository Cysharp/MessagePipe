using System;
using System.Threading.Tasks;


namespace MessagePipe;

public static class MessagePipeMediatorExtensions
{
    public static Func<TRequest, IRequestHandler<TRequest, TResponse>, TResponse> PrepareHandler<TRequest, TResponse>(
        this IMessagePipeMediator mediator)
        where TRequest: class
        where TResponse: class
    {
        var handler = mediator.GetHandler<TRequest, TResponse>();

        return (request, handler) =>
            handler.Invoke(request);
    }
    
    
    public static Func<TRequest, IAsyncRequestHandler<TRequest, TResponse>, ValueTask<TResponse>> PrepareAsyncHandler<TRequest, TResponse>(
        this IMessagePipeMediator mediator)
        where TRequest: class
        where TResponse: class
    {
        var handler = mediator.GetAsyncHandler<TRequest, TResponse>();

        return async (request, handler) => await handler.InvokeAsync(request);
    }
}
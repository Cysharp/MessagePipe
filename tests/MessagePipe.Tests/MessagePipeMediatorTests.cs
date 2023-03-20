using System;
using System.Threading;
using FluentAssertions;
using System.Threading.Tasks;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MessagePipe.Tests;

public class MessagePipeMediatorTests
{

    [Fact]
    public void MessagePipeMediator_Send_Standard()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<MyRequest, MyResponse>, MyRequesthandler>();
        services.AddSingleton<IMessagePipeMediator, MessagePipeMediator>();
        var p = services.BuildServiceProvider();
        var mediator = p.GetRequiredService<IMessagePipeMediator>();

        // Act
        var result = mediator.Send<MyRequest, MyResponse>(new MyRequest { Question = "This works?" });

        // Assert
        result.Answer.Should().Be("The answer to your question 'This works?' is: Yes!");
    }

    
    [Fact]
    public void MessagePipeMediator_PrepareHandler_Standard()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<MyRequest, MyResponse>, MyRequesthandler>();
        services.AddSingleton<IMessagePipeMediator, MessagePipeMediator>();
        var p = services.BuildServiceProvider();
        var mediator = p.GetRequiredService<IMessagePipeMediator>();
        
        mediator.PrepareMyHandler();
        var handler = mediator.GetHandler<MyRequest, MyResponse>();
        
        // Act
        var response = handler.ExecuteMyPreparedHandler(new MyRequest{ Question = "Are you wel prepared?"});

        // Assert
        response.Answer.Should().Be("The answer to your question 'Are you wel prepared?' is: Yes!");
    }
    
    
    [Fact]
    public async Task MessagePipeMediator_PrepareAsyncHandler_Standard()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IAsyncRequestHandler<MyRequest, MyResponse>, MyAsyncRequesthandler>();
        services.AddSingleton<IMessagePipeMediator, MessagePipeMediator>();
        var p = services.BuildServiceProvider();
        var mediator = p.GetRequiredService<IMessagePipeMediator>();
        
        mediator.PrepareMyAsyncHandler();
        var handler = mediator.GetAsyncHandler<MyRequest, MyResponse>();
        
        // Act
        var response = await handler.ExecuteMyPreparedAsyncHandler(new MyRequest{ Question = "Are you wel prepared?"});

        // Assert
        response.Answer.Should().Be("The answer to your question 'Are you wel prepared?' is: Yes!");
    }
    
    [Fact]
    public async Task MessagePipeMediator_SendAsync_Standard()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IAsyncRequestHandler<MyRequest, MyResponse>, MyAsyncRequesthandler>();
        services.AddSingleton<IMessagePipeMediator, MessagePipeMediator>();
        var p = services.BuildServiceProvider();
        var mediator = p.GetRequiredService<IMessagePipeMediator>();

        // Act
        var result = await mediator.SendAsync<MyRequest, MyResponse>(new MyRequest { Question = "This works?" });

        // Assert
        result.Answer.Should().Be("The answer to your question 'This works?' is: Yes!");
    }
}

public class MyRequest
{
    public string? Question { get; set; }
}

public class MyResponse
{
    public string? Answer { get; set; }
}

public class MyRequesthandler : IRequestHandler<MyRequest, MyResponse>
{
    public MyResponse Invoke(MyRequest request)
    {
        var result = $"The answer to your question '{request.Question}' is: Yes!";
        return new MyResponse { Answer = result };
    }
}

public class MyAsyncRequesthandler : IAsyncRequestHandler<MyRequest, MyResponse>
{
    public async ValueTask<MyResponse> InvokeAsync(MyRequest request, CancellationToken cancellationToken = default)
    {
        string result = $"The answer to your question '{request.Question}' is: Yes!";
        
        return new MyResponse { Answer = result };
    }
}

public static class MyHandlerExtensions
{
    private static Func<MyRequest, IRequestHandler<MyRequest, MyResponse>, MyResponse>? _myPreparedHandler;
    private static Func<MyRequest, IAsyncRequestHandler<MyRequest, MyResponse>, ValueTask<MyResponse>>? _myPreparedAsyncHandler;

    public static Func<MyRequest, IRequestHandler<MyRequest, MyResponse>, MyResponse>?
        PrepareMyHandler(this IMessagePipeMediator mediator)
    {
        _myPreparedHandler = mediator.PrepareHandler<MyRequest, MyResponse>();
        return _myPreparedHandler;
    }
    
    public static MyResponse ExecuteMyPreparedHandler(
        this IRequestHandler<MyRequest, MyResponse> handler, 
        MyRequest request)
    {
        if (_myPreparedHandler == null)
        {
            throw new ArgumentNullException(nameof(_myPreparedHandler),
                "Handler is not prepared yet: Call the appropriate PrepareHandler method first!");
        }
        
        return _myPreparedHandler(request, handler);
    }
    
    
    public static Func<MyRequest, IAsyncRequestHandler<MyRequest, MyResponse>, ValueTask<MyResponse>>?
        PrepareMyAsyncHandler(this IMessagePipeMediator mediator)
    {
        _myPreparedAsyncHandler = mediator.PrepareAsyncHandler<MyRequest, MyResponse>();
        return _myPreparedAsyncHandler;
    }
    
    public static async ValueTask<MyResponse> ExecuteMyPreparedAsyncHandler(
        this IAsyncRequestHandler<MyRequest, MyResponse> handler, 
        MyRequest request)
    {
        if (_myPreparedAsyncHandler == null)
        {
            throw new ArgumentNullException(nameof(_myPreparedAsyncHandler),
                "Handler is not prepared yet: Call the appropriate PrepareAsyncHandler method first!");
        }
        
        return await _myPreparedAsyncHandler(request, handler);
    }
}

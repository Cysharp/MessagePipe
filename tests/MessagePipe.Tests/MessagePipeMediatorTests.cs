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
    public void MessagePipeMediator_Sync_Standard()
    {
        var services = new ServiceCollection();

        services.AddScoped<IRequestHandler<MyRequest, MyResponse>, MyRequesthandler>();
        
        services.AddSingleton<IMessagePipeMediator, MessagePipeMediator>();

        var p = services.BuildServiceProvider();

        var mediator = p.GetRequiredService<IMessagePipeMediator>();

        var result = mediator.Send<MyRequest, MyResponse>(new MyRequest { Question = "This works?" });

        result.Answer.Should().Be("The answer to your question 'This works?' is: Yes!");
    }
    
    [Fact]
    public async Task MessagePipeMediator_Async_Standard()
    {
        var services = new ServiceCollection();

        services.AddScoped<IAsyncRequestHandler<MyRequest, MyResponse>, MyAsyncRequesthandler>();
        
        services.AddSingleton<IMessagePipeMediator, MessagePipeMediator>();

        var p = services.BuildServiceProvider();

        var mediator = p.GetRequiredService<IMessagePipeMediator>();

        var result = await mediator.SendAsync<MyRequest, MyResponse>(new MyRequest { Question = "This works?" });

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

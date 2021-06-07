using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// for check diagnostics, modify namespace.
namespace __MessagePipe.Tests
{

    public class RequestHandlerAsyncTest
    {

        [Fact]
        public async Task TestAsyncHandling()
        {
            var provider = TestHelper.BuildServiceProvider();
            var pingHandler = provider.GetRequiredService<IAsyncRequestHandler<Ping, Pong>>();

            var pong = await pingHandler.InvokeAsync(new Ping("myon!"));

        }

        [Fact]
        public void TestCancellation()
        {
            var provider = TestHelper.BuildServiceProvider();
            var pingHandler = provider.GetRequiredService<IAsyncRequestHandler<Ping, Pong>>();

            CancellationTokenSource source = new();
            var token = source.Token;

            source.Cancel();
            pingHandler.Awaiting(x => x.InvokeAsync(new Ping("hoge"), token)).Should().Throw<OperationCanceledException>();

        }


        class Ping
        {
            public string AnyValue;
            public Ping(string anyValue)
            {
                AnyValue = anyValue;
            }
        }
        class Pong
        {
            public string AnyValue;
            public Pong(string anyValue)
            {
                AnyValue = anyValue;
            }
        }


        class AsyncPingPongHandler : IAsyncRequestHandler<Ping, Pong>
        {
            public ValueTask<Pong> InvokeAsync(Ping request, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(new Pong(request.AnyValue + request.AnyValue));
            }
        }
    }
}

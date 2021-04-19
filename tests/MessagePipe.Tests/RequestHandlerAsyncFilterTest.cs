using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Runtime.CompilerServices;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace __MessagePipe.Tests
{
    public class RequestHandlerAsyncFilterTest
    {
        public static string Message;
        [Fact]
        public async Task AsyncFilterTest()
        {
            var provider = TestHelper.BuildServiceProvider();
            var handler = provider.GetRequiredService<IAsyncRequestHandler<Ping, Pong>>();

            var validPing = new Ping("ping");
            var nullPing = new Ping(null);

            var validPong = await handler.InvokeAsync(validPing);
            var nullPong = await handler.InvokeAsync(nullPing);

            validPong.AnyValue.Should().Be("ping");
            nullPong.AnyValue.Should().Be("ping was null.");
        }

        class AsyncPingPongHandlerFilter : AsyncRequestHandlerFilter
        {

            public override ValueTask<TResponse> InvokeAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, ValueTask<TResponse>> next)
            {
                var req = Unsafe.As<TRequest, Ping>(ref request);
                if (req.Value == null)
                {
                    var ret = new Pong("ping was null.");
                    return ValueTask.FromResult(Unsafe.As<Pong, TResponse>(ref ret));
                }
                return ValueTask.FromResult(Unsafe.As<Ping, TResponse>(ref req));
            }
        }

        class Ping
        {
            public string Value;

            public Ping(string anyValue)
            {
                Value = anyValue;
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
        [AsyncRequestHandlerFilter(typeof(AsyncPingPongHandlerFilter))]
        class AsyncPingPongHandler : IAsyncRequestHandler<Ping, Pong>
        {
            public ValueTask<Pong> InvokeAsync(Ping request, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(new Pong(request.Value));
            }
        }
    }
}


#pragma warning disable CS1998

using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace __MessagePipe.Tests
{
    public class RequestHandlerAsyncFilterTest
    {
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

        class AsyncPingPongHandlerFilter : AsyncRequestHandlerFilter<Ping, Pong>
        {

            public override async ValueTask<Pong> InvokeAsync(Ping request, CancellationToken cancellationToken, Func<Ping, CancellationToken, ValueTask<Pong>> next)
            {
                if (request.Value == null)
                {
                    var ret = new Pong("ping was null.");
                    return ret;
                }
                return new Pong(request.Value);
            }
        }

        public class Ping
        {
            public string? Value;

            public Ping(string? anyValue)
            {
                Value = anyValue;
            }
        }
        public class Pong
        {
            public string? AnyValue;

            public Pong(string? anyValue)
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

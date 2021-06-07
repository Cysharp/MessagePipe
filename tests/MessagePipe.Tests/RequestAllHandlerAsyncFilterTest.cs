#pragma warning disable CS1998

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace __MessagePipe.Tests
{
    public class RequestAllHandlerAsyncFilterTest
    {
        [Fact]
        public async Task AsyncFilterTest()
        {
            var provider = TestHelper.BuildServiceProvider();
            var handler = provider.GetRequiredService<IAsyncRequestAllHandler<Ping, Pong>>();

            var nullPongs = await handler.InvokeAllAsync(new Ping(null));

            nullPongs.Should().ContainEquivalentOf(new Pong("ping was null."));
            nullPongs.Should().ContainEquivalentOf(new Pong("ping was null!"));
        }

        [Fact]
        public async Task AsyncFilterLazyTest()
        {
            var provider = TestHelper.BuildServiceProvider();
            var handler = provider.GetRequiredService<IAsyncRequestAllHandler<Ping, Pong>>();

            var nullPongs = await handler.InvokeAllLazyAsync(new Ping(null)).ToArrayAsync();

            nullPongs.Should().ContainEquivalentOf(new Pong("ping was null."));
            nullPongs.Should().ContainEquivalentOf(new Pong("ping was null!"));
        }

        public class PingPongHandlerAsyncFilter : AsyncRequestHandlerFilter<Ping, Pong>
        {
            public override async ValueTask<Pong> InvokeAsync(Ping request, CancellationToken cancellationToken, Func<Ping, CancellationToken, ValueTask<Pong>> next)
            {
                if (request.AnyValue == null)
                {
                    var ret = new Pong("ping was null.");
                    return ret;
                }
                return new Pong(request.AnyValue);
            }
        }
        public class PingPongHandlerAsyncFilter2 : AsyncRequestHandlerFilter<Ping, Pong>
        {
            public override async ValueTask<Pong> InvokeAsync(Ping request, CancellationToken cancellationToken, Func<Ping, CancellationToken, ValueTask<Pong>> next)
            {
                if (request.AnyValue == null)
                {
                    var ret = new Pong("ping was null!");
                    return ret;
                }
                return new Pong(request.AnyValue);
            }
        }

        public class Ping
        {
            public string? AnyValue;
            public Ping(string? anyValue)
            {
                AnyValue = anyValue;
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

        [AsyncRequestHandlerFilter(typeof(PingPongHandlerAsyncFilter))]
        class AsyncPingPongHandler : IAsyncRequestHandler<Ping, Pong>
        {
            public ValueTask<Pong> InvokeAsync(Ping request, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(new Pong(request.AnyValue));
            }
        }

        [AsyncRequestHandlerFilter(typeof(PingPongHandlerAsyncFilter2))]
        class AsyncPingPongHandler2 : IAsyncRequestHandler<Ping, Pong>
        {
            public ValueTask<Pong> InvokeAsync(Ping request, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(new Pong(request.AnyValue + request.AnyValue));
            }
        }
    }
}

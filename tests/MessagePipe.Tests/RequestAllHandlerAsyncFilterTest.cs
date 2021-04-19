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
    public class RequestAllHandlerAsyncFilterTest
    {
        public static string Message;
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

        public class PingPongHandlerAsyncFilter : AsyncRequestHandlerFilter
        {
            public override ValueTask<TResponse> InvokeAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, ValueTask<TResponse>> next)
            {
                var req = Unsafe.As<TRequest, Ping>(ref request);
                if (req.AnyValue == null)
                {
                    var ret = new Pong("ping was null.");
                    return ValueTask.FromResult(Unsafe.As<Pong, TResponse>(ref ret));
                }
                return ValueTask.FromResult(Unsafe.As<Ping, TResponse>(ref req));
            }
        }
        public class PingPongHandlerAsyncFilter2 : AsyncRequestHandlerFilter
        {
            public override ValueTask<TResponse> InvokeAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, ValueTask<TResponse>> next)
            {
                var req = Unsafe.As<TRequest, Ping>(ref request);
                if (req.AnyValue == null)
                {
                    var ret = new Pong("ping was null!");
                    return ValueTask.FromResult(Unsafe.As<Pong, TResponse>(ref ret));
                }
                return ValueTask.FromResult(Unsafe.As<Ping, TResponse>(ref req));
            }
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

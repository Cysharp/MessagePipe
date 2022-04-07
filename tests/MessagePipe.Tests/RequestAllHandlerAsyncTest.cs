using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// for check diagnostics, modify namespace.
namespace __MessagePipe.Tests
{

    public class RequestAllHandlerASyncTest
    {
        [Fact]
        public async Task TestAsyncHandling()
        {

            var provider = TestHelper.BuildServiceProvider();
            var pingHandler = provider.GetRequiredService<IAsyncRequestAllHandler<Ping, Pong>>();

            var pongs = await pingHandler.InvokeAllAsync(new Ping("myon!"));

            pongs.Should().ContainEquivalentOf(new Pong("myon!myon!"));
            pongs.Should().ContainEquivalentOf(new Pong("myon!"));
        }
        [Fact]
        public async Task TestLazyAsyncHandling()
        {

            var provider = TestHelper.BuildServiceProvider();
            var pingHandler = provider.GetRequiredService<IAsyncRequestAllHandler<Ping, Pong>>();

            var pongs = await pingHandler.InvokeAllLazyAsync(new Ping("myon!")).ToArrayAsync();

            pongs.Should().ContainEquivalentOf(new Pong("myon!myon!"));
            pongs.Should().ContainEquivalentOf(new Pong("myon!"));
        }
        [Fact]
        public void TestCancellation()
        {
            var provider = TestHelper.BuildServiceProvider();
            var pingHandler = provider.GetRequiredService<IAsyncRequestAllHandler<Ping, Pong>>();

            var source = new CancellationTokenSource();
            var token = source.Token;

            source.Cancel();
            var pongs = pingHandler.Awaiting(x => x.InvokeAllAsync(new Ping("hoge"), token)).Should()
                .ThrowAsync<OperationCanceledException>();

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
                return ValueTask.FromResult(new Pong(request.AnyValue));
            }
        }

        class AsyncPingPongTwiceHandler : IAsyncRequestHandler<Ping, Pong>
        {
            public ValueTask<Pong> InvokeAsync(Ping request, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(new Pong(request.AnyValue + request.AnyValue));
            }
        }
    }
}

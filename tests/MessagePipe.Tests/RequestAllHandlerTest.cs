using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Xunit;

// for check diagnostics, modify namespace.
namespace __MessagePipe.Tests
{

    public class RequestAllHandlerTest
    {
        [Fact]
        public void TestHandling()
        {
            var provider = TestHelper.BuildServiceProvider();
            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var pingHandler = provider.GetRequiredService<IRequestAllHandler<Ping, Pong>>();

            var pongs = pingHandler.InvokeAll(new Ping("myon!"));

            pongs.Should().BeEquivalentTo(new Pong[] { new Pong("myon!"), new Pong("myon!myon!") });

        }
        [Fact]
        public void TestLazyHandling()
        {
            var provider = TestHelper.BuildServiceProvider();
            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var pingHandler = provider.GetRequiredService<IRequestAllHandler<Ping, Pong>>();

            var pongs = pingHandler.InvokeAllLazy(new Ping("myon!"));

            pongs.Should().BeEquivalentTo(new Pong[] { new Pong("myon!"), new Pong("myon!myon!") });

        }
        [Fact]
        public void TestCancellation()
        {
            var provider = TestHelper.BuildServiceProvider();
            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var pingHandler = provider.GetRequiredService<IRequestAllHandler<NotImplementedPing, NotImplementedPong>>();

            pingHandler.Invoking(x => x.InvokeAll(new NotImplementedPing())).Should().Throw<NotImplementedException>();
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
        class PingPongHandler : IRequestHandler<Ping, Pong>
        {
            public Pong Invoke(Ping request)
            {
                return new Pong(request.AnyValue);
            }
        }
        class PingPongTwiceHandler : IRequestHandler<Ping, Pong>
        {
            public Pong Invoke(Ping request)
            {
                return new Pong(request.AnyValue + request.AnyValue);
            }
        }
        class NotImplementedPing { }
        class NotImplementedPong { }
        class NotImplementedPingPongHandler : IRequestHandler<NotImplementedPing, NotImplementedPong>
        {
            public NotImplementedPong Invoke(NotImplementedPing request)
            {
                throw new NotImplementedException();
            }
        }
    }
}

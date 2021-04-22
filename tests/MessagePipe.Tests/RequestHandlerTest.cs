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

    public class RequestHandlerTest
    {
        [Fact]
        public void TestHandling()
        {
            var provider = TestHelper.BuildServiceProvider();
            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var pingHandler = provider.GetRequiredService<IRequestHandler<Ping, Pong>>();

            var pong = pingHandler.Invoke(new Ping("myon!"));

            pong.Value.Should().Be("myon!");
        }
        [Fact]
        public void TestCancellation()
        {
            var provider = TestHelper.BuildServiceProvider();
            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var pingHandler = provider.GetRequiredService<IRequestHandler<NotImplementedPing, NotImplementedPong>>();

            pingHandler.Invoking(x => x.Invoke(new NotImplementedPing())).Should().Throw<NotImplementedException>();
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
            public string Value;

            public Pong(string anyValue)
            {
                Value = anyValue;
            }
        }

        class PingPongHandler : IRequestHandler<Ping, Pong>
        {
            public Pong Invoke(Ping request)
            {
                return new Pong(request.Value);
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

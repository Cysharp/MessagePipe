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
            var pingHandler = provider.GetRequiredService<IRequestHandler<Ping, Pong>>();

            var pong = pingHandler.Invoke(new Ping("myon!"));

            Assert.Equal("myon!", pong.Value);
        }
        [Fact]
        public void TestCancellation()
        {
            var provider = TestHelper.BuildServiceProvider();
            var pingHandler = provider.GetRequiredService<IRequestHandler<NotImplementedPing, NotImplementedPong>>();

            Assert.Throws<NotImplementedException>(() => pingHandler.Invoke(new NotImplementedPing()));
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

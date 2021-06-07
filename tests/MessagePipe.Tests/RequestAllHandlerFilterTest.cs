using System;
using Xunit;
using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace __MessagePipe.Tests
{
    public class RequestAllHandlerFilterTest
    {
        [Fact]
        public void FilterTest()
        {
            var provider = TestHelper.BuildServiceProvider();
            var handler = provider.GetRequiredService<IRequestAllHandler<Ping, Pong>>();

            var nullPing = new Ping(null);

            var validPong = handler.InvokeAll(nullPing);
            var nullPong = handler.InvokeAll(nullPing);

            validPong.Should().ContainEquivalentOf(new Pong("ping was null."));
            nullPong.Should().ContainEquivalentOf(new Pong("ping was null!"));
        }

        public class PingPongHandlerFilter : RequestHandlerFilter<Ping, Pong>
        {
            public override Pong Invoke(Ping request, Func<Ping, Pong> next)
            {
                if (request.AnyValue == null)
                {
                    var ret = new Pong("ping was null.");
                    return ret;
                }
                return new Pong(request.AnyValue);
            }
        }
        public class PingPongHandlerFilter2 : RequestHandlerFilter<Ping, Pong>
        {
            public override Pong Invoke(Ping request, Func<Ping, Pong> next)
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
        [RequestHandlerFilter(typeof(PingPongHandlerFilter))]
        class PingPongHandler : IRequestHandler<Ping, Pong>
        {
            public Pong Invoke(Ping request)
            {
                return new Pong(request.AnyValue);
            }
        }
        [RequestHandlerFilter(typeof(PingPongHandlerFilter2))]
        class PingPongHandler2 : IRequestHandler<Ping, Pong>
        {
            public Pong Invoke(Ping request)
            {
                return new Pong(request.AnyValue);
            }
        }
    }
}

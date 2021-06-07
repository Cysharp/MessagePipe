using System;
using Xunit;
using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace __MessagePipe.Tests
{
    public class RequestHandlerFilterTest
    {
        [Fact]
        public void FilterTest()
        {
            var provider = TestHelper.BuildServiceProvider();
            var handler = provider.GetRequiredService<IRequestHandler<Ping, Pong>>();

            var validPing = new Ping("ping");
            var nullPing = new Ping(null);

            var validPong = handler.Invoke(validPing);
            var nullPong = handler.Invoke(nullPing);

            validPong.AnyValue.Should().Be("ping");
            nullPong.AnyValue.Should().Be("ping was null.");
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
    }
}

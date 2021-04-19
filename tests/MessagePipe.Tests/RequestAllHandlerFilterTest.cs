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

namespace __MessagePipe.Tests
{
    public class RequestAllHandlerFilterTest
    {
        public static string Message;
        [Fact]
        public void FilterTest()
        {
            var provider = TestHelper.BuildServiceProvider();
            var handler = provider.GetRequiredService<IRequestAllHandler<Ping, Pong>>();

            var nullPing = new Ping(null);

            var validPong = handler.InvokeAll(nullPing);
            var nullPong = handler.InvokeAll(nullPing);

            validPong.Should().ContainEquivalentOf(new Pong("ping was null."));
            validPong.Should().ContainEquivalentOf(new Pong("ping was null!"));
        }

        public class PingPongHandlerFilter : RequestHandlerFilter
        {
            public override TResponse Invoke<TRequest, TResponse>(TRequest request, Func<TRequest, TResponse> next)
            {
                var req = Unsafe.As<TRequest, Ping>(ref request);
                if (req.AnyValue == null)
                {
                    var ret = new Pong("ping was null.");
                    return Unsafe.As<Pong, TResponse>(ref ret);
                }
                return Unsafe.As<Ping, TResponse>(ref req);
            }
        }
        public class PingPongHandlerFilter2 : RequestHandlerFilter
        {
            public override TResponse Invoke<TRequest, TResponse>(TRequest request, Func<TRequest, TResponse> next)
            {
                var req = Unsafe.As<TRequest, Ping>(ref request);
                if (req.AnyValue == null)
                {
                    var ret = new Pong("ping was null!");
                    return Unsafe.As<Pong, TResponse>(ref ret);
                }
                return Unsafe.As<Ping, TResponse>(ref req);
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

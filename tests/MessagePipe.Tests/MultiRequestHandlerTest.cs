using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// for check diagnostics, modify namespace.
namespace __MessagePipe.Tests
{

    public class MultiRequestHandlerTest
    {
        [Fact]
        public void SyncRegister()
        {
            var provider = TestHelper.BuildServiceProvider3(x =>
            {
                x.EnableAutoRegistration = false;
            }, x =>
            {
                x.AddRequestHandler<MultiHandler>();
            });

            var handler1 = provider.GetRequiredService<IRequestHandler<Request1, Response1>>();
            var handler2 = provider.GetRequiredService<IRequestHandler<Request2, Response2>>();

            var req1 = new Request1();
            handler1.Invoke(req1).Request.Should().Be(req1);

            var req2 = new Request2();
            handler2.Invoke(req2).Request.Should().Be(req2);
        }

        [Fact]
        public async Task AsyncRegister()
        {
            var provider = TestHelper.BuildServiceProvider3(x =>
            {
                x.EnableAutoRegistration = false;
            }, x =>
            {
                x.AddAsyncRequestHandler<MultiHandler>();
            });

            var handler1 = provider.GetRequiredService<IAsyncRequestHandler<Request1, Response1>>();
            var handler2 = provider.GetRequiredService<IAsyncRequestHandler<Request2, Response2>>();

            var req1 = new Request1();
            (await handler1.InvokeAsync(req1)).Request.Should().Be(req1);

            var req2 = new Request2();
            (await handler2.InvokeAsync(req2)).Request.Should().Be(req2);
        }

        class MultiHandler :
            IRequestHandler<Request1, Response1>,
            IRequestHandler<Request2, Response2>,
            IAsyncRequestHandler<Request1, Response1>,
            IAsyncRequestHandler<Request2, Response2>
        {
            public Response1 Invoke(Request1 request)
            {
                return new Response1(request);
            }

            public Response2 Invoke(Request2 request)
            {
                return new Response2(request);
            }

            public ValueTask<Response1> InvokeAsync(Request1 request, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(new Response1(request));
            }

            public ValueTask<Response2> InvokeAsync(Request2 request, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(new Response2(request));
            }
        }

        class Request1 { }
        class Request2 { }
        class Response1
        {
            public Request1 Request { get; set; }

            public Response1(Request1 request)
            {
                this.Request = request;
            }
        }

        class Response2
        {
            public Request2 Request { get; set; }

            public Response2(Request2 request)
            {
                Request = request;
            }
        }
    }
}

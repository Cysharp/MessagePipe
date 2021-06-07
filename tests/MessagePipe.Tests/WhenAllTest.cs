using FluentAssertions;
using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MessagePipe.Tests
{
    public class WhenAllTest
    {
        [Fact]
        public async Task OkWhenAll()
        {
            var handlers = Enumerable.Range(1, 10)
                .Select((x, i) => (i % 2 == 0) ? null : new AsyncHandler { DelaySpan = (TimeSpan.FromMilliseconds(1000 - (i * 100))) })
                .Select((x, i) => (i == 3) ? (IAsyncMessageHandler<int>)new CompletedHandler() : x)
                .ToArray();

            await new AsyncHandlerWhenAll<int>(handlers, 999, CancellationToken.None);

            handlers.OfType<ICheck>().Select(x => x.ReceivedMessage).Should().Equal(999, 999, 999, 999, 999);

            // again(use pool?)
            handlers = Enumerable.Range(1, 10)
              .Select((x, i) => (i % 2 == 0) ? null : new AsyncHandler { DelaySpan = TimeSpan.FromSeconds(1) })
              .Select((x, i) => (i == 3) ? (IAsyncMessageHandler<int>)new CompletedHandler() : x)
              .ToArray();

            await new AsyncHandlerWhenAll<int>(handlers, 999, CancellationToken.None);

            handlers.OfType<ICheck>().Select(x => x.ReceivedMessage).Should().Equal(999, 999, 999, 999, 999);
        }

        [Fact]
        public async Task WithExceptionWhenAll()
        {
            var handlers = Enumerable.Range(1, 10)
                .Select((x, i) => (i % 2 == 0) ? null : new AsyncHandler { DelaySpan = TimeSpan.FromSeconds(1) })
                .Select((x, i) => (i == 3) ? (IAsyncMessageHandler<int>)new CompletedHandler() : x)
                .Select((x, i) => (i == 4) ? new ExceptionHandler() : x)
                .ToArray();

            await Assert.ThrowsAsync<WhenAllTestExteption>(async () =>
               await new AsyncHandlerWhenAll<int>(handlers, 999, CancellationToken.None));
        }

        [Fact]
        public async Task RequestWhenAll()
        {
            var handlers = Enumerable.Range(1, 10)
                .Select((x, i) => new AsyncHandler2 { DelaySpan = (TimeSpan.FromMilliseconds(1000 - (i * 100))) })
                .Select((x, i) => (i == 3) ? (IAsyncRequestHandler<int, int>)new CompletedHandler2() : x)
                .ToArray();

            var xs = await new AsyncRequestHandlerWhenAll<int, int>(handlers, 10, CancellationToken.None);

            xs.Should().Equal(100, 100, 100, 100, 100, 100, 100, 100, 100, 100);

            // again(use pool?)
            handlers = Enumerable.Range(1, 10)
               .Select((x, i) => new AsyncHandler2 { DelaySpan = (TimeSpan.FromMilliseconds(1000 - (i * 100))) })
               .Select((x, i) => (i == 3) ? (IAsyncRequestHandler<int, int>)new CompletedHandler2() : x)
               .ToArray();


            xs = await new AsyncRequestHandlerWhenAll<int, int>(handlers, 10, CancellationToken.None);

            xs.Should().Equal(100, 100, 100, 100, 100, 100, 100, 100, 100, 100);
        }

        [Fact]
        public async Task RequestWhenAllWithException()
        {
            var handlers = Enumerable.Range(1, 10)
                .Select((x, i) => new AsyncHandler2 { DelaySpan = (TimeSpan.FromMilliseconds(1000 - (i * 100))) })
                .Select((x, i) => (i == 3) ? (IAsyncRequestHandler<int, int>)new CompletedHandler2() : x)
                .Select((x, i) => (i == 4) ? new ExceptionHandler2() : x)
                .ToArray();

            await Assert.ThrowsAsync<WhenAllTestExteption>(async () =>
               await new AsyncRequestHandlerWhenAll<int, int>(handlers, 999, CancellationToken.None));
        }
    }

    public interface ICheck
    {
        int ReceivedMessage { get; }
    }

    class WhenAllTestExteption : Exception
    {

    }

    class AsyncHandler : IAsyncMessageHandler<int>, ICheck
    {
        public TimeSpan DelaySpan { get; set; }
        public int ReceivedMessage { get; private set; }

        public async ValueTask HandleAsync(int message, CancellationToken cancellationToken)
        {
            await Task.Delay(DelaySpan);
            ReceivedMessage = message;
        }
    }

    class ExceptionHandler : IAsyncMessageHandler<int>, ICheck
    {
        public int ReceivedMessage { get; private set; }

        public async ValueTask HandleAsync(int message, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(400));
            throw new WhenAllTestExteption();
        }
    }

    class CompletedHandler : IAsyncMessageHandler<int>, ICheck
    {
        public int ReceivedMessage { get; private set; }

        public ValueTask HandleAsync(int message, CancellationToken cancellationToken)
        {
            ReceivedMessage = message;
            return default;
        }
    }


    class AsyncHandler2 : IAsyncRequestHandler<int, int>
    {
        public TimeSpan DelaySpan { get; set; }

        public async ValueTask<int> InvokeAsync(int message, CancellationToken cancellationToken)
        {
            await Task.Delay(DelaySpan);
            return message * message;
        }
    }

    class ExceptionHandler2 : IAsyncRequestHandler<int, int>
    {
        public async ValueTask<int> InvokeAsync(int message, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(400));
            throw new WhenAllTestExteption();
        }
    }

    class CompletedHandler2 : IAsyncRequestHandler<int, int>
    {
        public ValueTask<int> InvokeAsync(int message, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(message * message);
        }
    }
}

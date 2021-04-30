using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

// for check diagnostics, modify namespace.
namespace __MessagePipe.Tests
{
    public class PubSubAsyncFilterTest
    {
        [Fact]
        public void SimplePushAsyncWithFilter()
        {
            var provider = TestHelper.BuildServiceProvider();

            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var p = provider.GetRequiredService<IAsyncPublisher<string>>();
            var s = provider.GetRequiredService<IAsyncSubscriber<string>>();

            var result = new List<string>();
#pragma warning disable CS1998
            var d = s.Subscribe(async (x,ct) => result.Add("d:" + x));
            var f = s.Subscribe(async (x, ct) => result.Add("f:" + x), new AsyncReverseFilter());
            var ff = s.Subscribe(async (x, ct) => result.Add("f:" + x), new AsyncReverseFilter(), new AsyncReverseFilter());
#pragma warning restore CS1998

            info.SubscribeCount.Should().Be(3);

            // use BeEquivalentTo, allow different order

            p.Publish("one");
            result.Should().BeEquivalentTo("d:one", "f:eno", "f:one");
            result.Clear();

            d.Dispose();

            p.Publish("two");
            result.Should().BeEquivalentTo("f:owt", "f:two");
            result.Clear();
            f.Dispose();

            p.Publish("three");
            result.Should().BeEquivalentTo("f:three");
            result.Clear();
            ff.Dispose();
            info.SubscribeCount.Should().Be(0);
        }
    }
    class AsyncReverseFilter : AsyncMessageHandlerFilter<string>
    {
        public override async ValueTask HandleAsync(string message, CancellationToken cancellationToken, System.Func<string, CancellationToken, ValueTask> next)
        {
            var filtered = string.Concat(message.Reverse());
            await next(filtered, cancellationToken);
        }
    }
}

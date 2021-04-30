using FluentAssertions;
using MessagePipe;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xunit;
using System.Linq;

// for check diagnostics, modify namespace.
namespace __MessagePipe.Tests
{
    public class PubSubFilterTest
    {
        [Fact]
        public void SimplePushWithFilter()
        {
            var provider = TestHelper.BuildServiceProvider();

            var info = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var p = provider.GetRequiredService<IPublisher<string>>();
            var s = provider.GetRequiredService<ISubscriber<string>>();

            var result = new List<string>();
            var d = s.Subscribe(x => result.Add("d:" + x));
            var f = s.Subscribe(x => result.Add("f:" + x),new ReverseFilter());
            var ff = s.Subscribe(x => result.Add("f:" + x), new ReverseFilter(), new ReverseFilter());

            info.SubscribeCount.Should().Be(3);

            // use BeEquivalentTo, allow different order

            p.Publish("one");
            result.Should().BeEquivalentTo("d:one", "f:eno", "f:one");
            result.Clear();

            d.Dispose();

            p.Publish("two");
            result.Should().BeEquivalentTo("f:owt","f:two");
            result.Clear();
            f.Dispose();

            p.Publish("three");
            result.Should().BeEquivalentTo("f:three");
            result.Clear();
            ff.Dispose();
            info.SubscribeCount.Should().Be(0);

            var g = s.Subscribe(x => 
            result.Add("g:" + x),new GenericFilter<string>());
            p.Publish("four");
            result.Should().BeEquivalentTo("g:four");
            FilterResultContainer.MessageBefore.Should().Be("Before");
            FilterResultContainer.MessageAfter.Should().Be("After");
        }
    }
    class ReverseFilter : MessageHandlerFilter<string>
    {
        public override void Handle(string message, System.Action<string> next)
        {
            var filtered = string.Concat(message.Reverse());
            next(filtered);
        }
    }
    class GenericFilter<T> : MessageHandlerFilter<T>
    {

        public override void Handle(T message, System.Action<T> next)
        {
            FilterResultContainer.MessageBefore = "Before";
            next(message);
            FilterResultContainer.MessageAfter = "After";
        }
    }
    static class FilterResultContainer
    {
        public static string MessageBefore;
        public static string MessageAfter;
    }
}

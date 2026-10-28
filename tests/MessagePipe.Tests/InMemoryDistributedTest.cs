using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePipe.Tests
{
    public class InMemoryDistributedTest
    {
        [Fact]
        public async Task SimplePush()
        {
            const string Key1 = "foo";
            const string Key2 = "bar";


            var provider = TestHelper.BuildServiceProvider2(x =>
            {
                x.AddInMemoryDistributedMessageBroker();
            });

            var p = provider.GetRequiredService<IDistributedPublisher<string, string>>();
            var s = provider.GetRequiredService<IDistributedSubscriber<string, string>>();

            var result = new List<string>();

            var d1 = await s.SubscribeAsync(Key1, (x) =>
            {
                result.Add("1:" + x);
            });
            var d2 = await s.SubscribeAsync(Key2, (x) => result.Add("2:" + x));
            var d3 = await s.SubscribeAsync(Key1, (x) => result.Add("3:" + x));

            // use BeEquivalentTo, allow different order

            await p.PublishAsync(Key1, "one");

            Assert.Equivalent(new[] { "1:one", "3:one" }, result, strict: true);
            result.Clear();

            await p.PublishAsync(Key2, "one");

            Assert.Equivalent(new[] { "2:one" }, result, strict: true);
            result.Clear();

            await d3.DisposeAsync();

            await p.PublishAsync(Key1, "two");
            Assert.Equivalent(new[] { "1:two" }, result, strict: true);
            result.Clear();

            await d1.DisposeAsync();
            await d2.DisposeAsync();

            await p.PublishAsync(Key1, "zero");
            await p.PublishAsync(Key2, "zero");

            Assert.Empty(result);
            result.Clear();
        }

    }
}

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MessagePipe.InProcess.Tests
{
    public class UdpTest
    {
        readonly ITestOutputHelper helper;

        public UdpTest(ITestOutputHelper testOutputHelper)
        {
            this.helper = testOutputHelper;
        }

        [Fact]
        public async Task SimpleIntInt()
        {
            var provider = TestHelper.BuildServiceProviderUdp("127.0.0.1", 1784, helper);
            using (provider as IDisposable)
            {
                var p1 = provider.GetRequiredService<IDistributedPublisher<int, int>>();
                var s1 = provider.GetRequiredService<IDistributedSubscriber<int, int>>();

                var result = new List<int>();
                await s1.SubscribeAsync(1, x =>
                {
                    result.Add(x);
                });

                var result2 = new List<int>();
                await s1.SubscribeAsync(4, x =>
                {
                    result2.Add(x);
                });

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...
                await p1.PublishAsync(1, 9999);
                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...
                await p1.PublishAsync(4, 888);
                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...
                await p1.PublishAsync(1, 4999);
                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...

                result.Should().Equal(9999, 4999);
                result2.Should().Equal(888);
            }
        }

        [Fact]
        public async Task SimpleStringString()
        {
            var provider = TestHelper.BuildServiceProviderUdp("127.0.0.1", 1436, helper);
            using (provider as IDisposable)
            {
                var p1 = provider.GetRequiredService<IDistributedPublisher<string, string>>();
                var s1 = provider.GetRequiredService<IDistributedSubscriber<string, string>>();

                var result = new List<string>();
                await s1.SubscribeAsync("hogemogeman", x =>
                {
                    result.Add(x);
                });

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...
                await p1.PublishAsync("hogemogeman", "abidatoxurusika");

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...

                result.Should().Equal("abidatoxurusika");
            }
        }
    }
}

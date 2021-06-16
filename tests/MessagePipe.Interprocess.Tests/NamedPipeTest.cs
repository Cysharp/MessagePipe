using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MessagePipe.Interprocess.Tests
{
    public class NamedPipeTest
    {
        readonly ITestOutputHelper helper;

        public NamedPipeTest(ITestOutputHelper testOutputHelper)
        {
            this.helper = testOutputHelper;
        }

        [Fact]
        public async Task SimpleIntInt()
        {
            var provider = TestHelper.BuildServiceProviderNamedPipe("foobar", helper);
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
        public async Task ConnectTwice()
        {
            var providerServer = TestHelper.BuildServiceProviderNamedPipe("zb", helper, asServer: false);
            var providerClient = TestHelper.BuildServiceProviderNamedPipe("zb", helper, asServer: false);
            var p1 = providerClient.GetRequiredService<IDistributedPublisher<int, int>>();
            var s1 = providerServer.GetRequiredService<IDistributedSubscriber<int, int>>();

            var result = new List<int>();
            await s1.SubscribeAsync(1, x =>
            {
                result.Add(x);
            });

            await p1.PublishAsync(1, 9999);
            var scope = (providerClient as IDisposable);

            await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...

            scope?.Dispose();

            await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...


            var providerClient2 = TestHelper.BuildServiceProviderNamedPipe("zb", helper, asServer: false);
            var p2 = providerClient2.GetRequiredService<IDistributedPublisher<int, int>>();

            await p2.PublishAsync(1, 4999);

            await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...
            result.Should().Equal(9999, 4999);

            (providerServer as IDisposable)?.Dispose();
            (providerClient2 as IDisposable)?.Dispose();
        }


        [Fact]
        public async Task SimpleStringString()
        {
            var provider = TestHelper.BuildServiceProviderNamedPipe("barbaz", helper);
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

        [Fact]
        public async Task HugeSizeTest()
        {
            var provider = TestHelper.BuildServiceProviderNamedPipe("z42fds", helper);
            using (provider as IDisposable)
            {
                var p1 = provider.GetRequiredService<IDistributedPublisher<string, string>>();
                var s1 = provider.GetRequiredService<IDistributedSubscriber<string, string>>();

                var result = new List<string>();
                await s1.SubscribeAsync("hogemogeman", x =>
                {
                    result.Add(x);
                });

                var ldata = new string('a', 99999);
                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...
                await p1.PublishAsync("hogemogeman", ldata);

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...

                result.Should().Equal(ldata);
            }
        }

        [Fact]
        public async Task MoreHugeSizeTest()
        {
            var provider = TestHelper.BuildServiceProviderNamedPipe("fdsew", helper);
            using (provider as IDisposable)
            {
                var p1 = provider.GetRequiredService<IDistributedPublisher<string, string>>();
                var s1 = provider.GetRequiredService<IDistributedSubscriber<string, string>>();

                var result = new List<string>();
                await s1.SubscribeAsync("hogemogeman", x =>
                {
                    result.Add(x);
                });

                var ldata1 = new string('a', 99999);
                var ldata2 = new string('b', 99999);
                var ldata3 = new string('c', 99999);
                var ldata = string.Concat(ldata1, ldata2, ldata3);
                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...
                await p1.PublishAsync("hogemogeman", ldata);

                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...

                result.Should().Equal(ldata);
            }
        }

        // remote request

        [Fact]
        public async Task RemoteRequestTest()
        {
            var provider = TestHelper.BuildServiceProviderNamedPipe("aewrw", helper);
            using (provider as IDisposable)
            {
                var remoteHandler = provider.GetRequiredService<IRemoteRequestHandler<int, string>>();

                var v = await remoteHandler.InvokeAsync(9999);
                v.Should().Be("ECHO:9999");

                var v2 = await remoteHandler.InvokeAsync(4444);
                v2.Should().Be("ECHO:4444");

                var ex = await Assert.ThrowsAsync<RemoteRequestException>(async () =>
                {
                    var v3 = await remoteHandler.InvokeAsync(-1);
                });
                ex.Message.Should().Contain("NO -1");
            }
        }
    }

    public class MyAsyncHandler : IAsyncRequestHandler<int, string>
    {
        public async ValueTask<string> InvokeAsync(int request, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1);
            if (request == -1)
            {
                throw new Exception("NO -1");
            }
            else
            {
                return "ECHO:" + request.ToString();
            }
        }
    }
}

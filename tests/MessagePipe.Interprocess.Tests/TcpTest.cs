using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MessagePipe.Interprocess.Tests
{
    public class TcpTest
    {
        readonly ITestOutputHelper helper;

        public TcpTest(ITestOutputHelper testOutputHelper)
        {
            this.helper = testOutputHelper;
        }

        [Fact]
        public async Task SimpleIntInt()
        {
            var provider = TestHelper.BuildServiceProviderTcp("127.0.0.1", 1784, helper);
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

                Assert.Equal(new[] { 9999, 4999 }, result);
                Assert.Equal(new[] { 888 }, result2);
            }
        }

        [Fact]
        public async Task TwoPublisher()
        {
            var provider = TestHelper.BuildServiceProviderTcp("127.0.0.1", 1992, helper, asServer: false);
            var provider2 = TestHelper.BuildServiceProviderTcp("127.0.0.1", 1992, helper, asServer: false);
            using (provider as IDisposable)
            using (provider2 as IDisposable)
            {
                var p1 = provider.GetRequiredService<IDistributedPublisher<int, int>>();
                var s1 = provider.GetRequiredService<IDistributedSubscriber<int, int>>();
                var p2 = provider2.GetRequiredService<IDistributedPublisher<int, int>>();

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
                await p2.PublishAsync(4, 888);
                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...
                await p2.PublishAsync(1, 4999);
                await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...

                Assert.Equal(new[] { 9999, 4999 }, result);
                Assert.Equal(new[] { 888 }, result2);
            }
        }

        [Fact]
        public async Task ConnectTwice()
        {
            var providerServer = TestHelper.BuildServiceProviderTcp("127.0.0.1", 1184, helper);
            var providerClient = TestHelper.BuildServiceProviderTcp("127.0.0.1", 1184, helper, asServer: false);
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


            var providerClient2 = TestHelper.BuildServiceProviderTcp("127.0.0.1", 1184, helper, asServer: false);
            var p2 = providerClient2.GetRequiredService<IDistributedPublisher<int, int>>();

            await p2.PublishAsync(1, 4999);

            await Task.Delay(TimeSpan.FromSeconds(1)); // wait for receive data...
            Assert.Equal(new[] { 9999, 4999 }, result);

            (providerServer as IDisposable)?.Dispose();
            (providerClient2 as IDisposable)?.Dispose();
        }


        [Fact]
        public async Task SimpleStringString()
        {
            var provider = TestHelper.BuildServiceProviderTcp("127.0.0.1", 1436, helper);
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

                Assert.Equal(new[] { "abidatoxurusika" }, result);
            }
        }

        [Fact]
        public async Task HugeSizeTest()
        {
            var provider = TestHelper.BuildServiceProviderTcp("127.0.0.1", 1436, helper);
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

                Assert.Equal(new[] { ldata }, result);
            }
        }

        [Fact]
        public async Task MoreHugeSizeTest()
        {
            var provider = TestHelper.BuildServiceProviderTcp("127.0.0.1", 1436, helper);
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

                Assert.Equal(new[] { ldata }, result);
            }
        }


        [Fact]
        public async Task RemoteRequestTest()
        {
            var provider = TestHelper.BuildServiceProviderTcp("127.0.0.1", 1355, helper, asServer: true);
            using (provider as IDisposable)
            {
                var remoteHandler = provider.GetRequiredService<IRemoteRequestHandler<int, string>>();

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

                var v = await remoteHandler.InvokeAsync(9999, cts.Token);
                Assert.Equal("ECHO:9999", v);

                var v2 = await remoteHandler.InvokeAsync(4444);
                Assert.Equal("ECHO:4444", v2);

                var ex = await Assert.ThrowsAsync<RemoteRequestException>(async () =>
                {
                    var v3 = await remoteHandler.InvokeAsync(-1);
                });
                Assert.Contains("NO -1", ex.Message);
            }
        }

        [Fact]
        public async Task MoreHugeSizeUdsTest()
        {
            var filePath = System.IO.Path.GetTempFileName();
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            try
            {
                var provider = TestHelper.BuildServiceProviderTcpWithUds(filePath, helper);
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

                    Assert.Equal(new[] { ldata }, result);
                }
            }
            finally
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }
        [Fact]
        public async Task RemoteRequestWithUdsTest()
        {
            var filePath = System.IO.Path.GetTempFileName();
            if(System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            try
            {
                var provider = TestHelper.BuildServiceProviderTcpWithUds(filePath, helper);
                using (provider as IDisposable)
                {
                    var remoteHandler = provider.GetRequiredService<IRemoteRequestHandler<int, string>>();

                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

                    var v = await remoteHandler.InvokeAsync(9999, cts.Token);
                    Assert.Equal("ECHO:9999", v);

                    var v2 = await remoteHandler.InvokeAsync(4444);
                    Assert.Equal("ECHO:4444", v2);

                    var ex = await Assert.ThrowsAsync<RemoteRequestException>(async () =>
                    {
                        var v3 = await remoteHandler.InvokeAsync(-1);
                    });
                    Assert.Contains("NO -1", ex.Message);
                }
            }
            finally
            {
                if(System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }
    }
}

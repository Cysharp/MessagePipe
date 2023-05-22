using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MessagePipe.Tests;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace MessagePipe.Redis.Tests;

public class ConnectionMultiplexerFactoryTest
{
    [Fact]
    public async Task CreateFromInstance()
    {
        var connection = TestHelper.GetLocalConnectionMultiplexer();
        var services = new ServiceCollection();
        services.AddMessagePipe();
        services.AddMessagePipeRedis(connection);
        var sp = services.BuildServiceProvider();

        var publisher = sp.GetRequiredService<IDistributedPublisher<string, string>>();
        var subscriber = sp.GetRequiredService<IDistributedSubscriber<string, string>>();
     
        var results = new List<string>();
        await using var _ = await subscriber.SubscribeAsync("Foo", x => results.Add(x));

        await publisher.PublishAsync("Foo", "Bar");
        await Task.Delay(250);
        results.FirstOrDefault().Should().Be("Bar");
    }

    [Fact]
    public async Task CreateFromGenericType()
    {
        var connection = TestHelper.GetLocalConnectionMultiplexer();
        var services = new ServiceCollection();
        services.AddSingleton<IConnectionMultiplexer>(connection);
        services.AddMessagePipe();
        services.AddMessagePipeRedis<TestConnectionMultiplexerFactory>();
        var sp = services.BuildServiceProvider();

        var publisher = sp.GetRequiredService<IDistributedPublisher<string, string>>();
        var subscriber = sp.GetRequiredService<IDistributedSubscriber<string, string>>();
     
        var results = new List<string>();
        await using var _ = await subscriber.SubscribeAsync("Foo", x => results.Add(x));

        await publisher.PublishAsync("Foo", "Bar");
        await Task.Delay(250);
        results.FirstOrDefault().Should().Be("Bar");
    }

    public class TestConnectionMultiplexerFactory : IConnectionMultiplexerFactory
    {
        readonly IConnectionMultiplexer connectionMultiplexer;

        public TestConnectionMultiplexerFactory(IConnectionMultiplexer connectionMultiplexer)
        {
            this.connectionMultiplexer = connectionMultiplexer;
        }

        public IConnectionMultiplexer GetConnectionMultiplexer<TKey>(TKey key)
        {
            return connectionMultiplexer;
        }
    }
}
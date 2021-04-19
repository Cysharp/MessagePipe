using MessagePipe.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;

namespace MessagePipe.Tests
{
    public static class TestHelper
    {
        public static IServiceProvider BuildServiceProvider()
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider BuildServiceProvider(Action<MessagePipeOptions> configure)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe(configure);
            return sc.BuildServiceProvider();
        }

        public static ConnectionMultiplexer GetLocalConnectionMultiplexer()
        {
            var c = ConfigurationOptions.Parse("localhost");
            c.ConnectTimeout = 1000;
            try
            {
                return StackExchange.Redis.ConnectionMultiplexer.Connect(c);
            }
            catch (RedisConnectionException ex)
            {
                throw new TimeoutException("Can not connect to redis, if you don't up redis in local, call 'docker-compose up' on project root.", ex);
            }
        }

        public static IServiceProvider BuildRedisServiceProvider(IConnectionMultiplexer connection)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe();
            sc.AddMessagePipeRedis(connection);
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider BuildRedisServiceProvider(IConnectionMultiplexer connection, Action<MessagePipeOptions> configure)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe(configure);
            sc.AddMessagePipeRedis(connection);
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider BuildRedisServiceProvider(IConnectionMultiplexer connection, Action<MessagePipeOptions> configure, Action<MessagePipeRedisOptions> redisConfigure)
        {
            var sc = new ServiceCollection();
            sc.AddMessagePipe(configure);
            sc.AddMessagePipeRedis(connection, redisConfigure);
            return sc.BuildServiceProvider();
        }
    }
}
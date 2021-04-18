using MessagePipe;
using MessagePipe.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionRedisExtensions
    {
        public static IServiceCollection AddMessagePipeRedis(this IServiceCollection services, ConnectionMultiplexer connectionMultiplexer)
        {
            return AddMessagePipeRedis(services, new SingleConnectionMultiplexerFactory(connectionMultiplexer), _ => { });
        }

        public static IServiceCollection AddMessagePipeRedis(this IServiceCollection services, IConnectionMultiplexerFactory connectionMultiplexerFactory)
        {
            return AddMessagePipeRedis(services, connectionMultiplexerFactory, _ => { });
        }

        public static IServiceCollection AddMessagePipeRedis(this IServiceCollection services, ConnectionMultiplexer connectionMultiplexer, Action<MessagePipeRedisOptions> configure)
        {
            return AddMessagePipeRedis(services, new SingleConnectionMultiplexerFactory(connectionMultiplexer), configure);
        }

        public static IServiceCollection AddMessagePipeRedis(this IServiceCollection services, IConnectionMultiplexerFactory connectionMultiplexerFactory, Action<MessagePipeRedisOptions> configure)
        {
            var options = new MessagePipeRedisOptions(connectionMultiplexerFactory);
            configure(options);
            services.AddSingleton(options); // add as singleton instance

            var scope = options.InstanceScope;
            Add(services, typeof(IDistributedPublisher<,>), typeof(RedisPublisher<,>), scope);
            Add(services, typeof(IDistributedSubscriber<,>), typeof(RedisSubscriber<,>), scope);

            return services;
        }

        static void Add(IServiceCollection services, Type serviceType, InstanceScope scope)
        {
            var lifetime = (scope == InstanceScope.Scoped) ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
            services.Add(new ServiceDescriptor(serviceType, serviceType, lifetime));
        }

        static void Add(IServiceCollection services, Type serviceType, Type implementationType, InstanceScope scope)
        {
            var lifetime = (scope == InstanceScope.Scoped) ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
            services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        }
    }
}

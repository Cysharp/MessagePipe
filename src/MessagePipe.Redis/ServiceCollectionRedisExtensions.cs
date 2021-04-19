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
        public static IServiceCollection AddMessagePipeRedis(this IServiceCollection services, IConnectionMultiplexer connectionMultiplexer)
        {
            return AddMessagePipeRedis(services, new SingleConnectionMultiplexerFactory(connectionMultiplexer), _ => { });
        }

        public static IServiceCollection AddMessagePipeRedis(this IServiceCollection services, IConnectionMultiplexerFactory connectionMultiplexerFactory)
        {
            return AddMessagePipeRedis(services, connectionMultiplexerFactory, _ => { });
        }

        public static IServiceCollection AddMessagePipeRedis(this IServiceCollection services, IConnectionMultiplexer connectionMultiplexer, Action<MessagePipeRedisOptions> configure)
        {
            return AddMessagePipeRedis(services, new SingleConnectionMultiplexerFactory(connectionMultiplexer), configure);
        }

        public static IServiceCollection AddMessagePipeRedis(this IServiceCollection services, IConnectionMultiplexerFactory connectionMultiplexerFactory, Action<MessagePipeRedisOptions> configure)
        {
            var options = new MessagePipeRedisOptions(connectionMultiplexerFactory);
            configure(options);
            services.AddSingleton(options); // add as singleton instance
            services.AddSingleton<IConnectionMultiplexerFactory>(options.ConnectionMultiplexerFactory);
            services.AddSingleton<IRedisSerializer>(options.RedisSerializer);

            var scope = options.InstanceScope;
            services.Add(typeof(IDistributedPublisher<,>), typeof(RedisPublisher<,>), scope);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(RedisSubscriber<,>), scope);

            return services;
        }

        static void Add(this IServiceCollection services, Type serviceType, InstanceScope scope)
        {
            var lifetime = (scope == InstanceScope.Scoped) ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
            services.Add(new ServiceDescriptor(serviceType, serviceType, lifetime));
        }

        static void Add(this IServiceCollection services, Type serviceType, Type implementationType, InstanceScope scope)
        {
            var lifetime = (scope == InstanceScope.Scoped) ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
            services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        }
    }
}

using MessagePipe;
using MessagePipe.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePipe
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

            services.Add(typeof(IDistributedPublisher<,>), typeof(RedisPublisher<,>), InstanceLifetime.Singleton);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(RedisSubscriber<,>), InstanceLifetime.Singleton);

            return services;
        }

        static void Add(this IServiceCollection services, Type serviceType, Type implementationType, InstanceLifetime scope)
        {
            var lifetime = (scope == InstanceLifetime.Scoped) ? ServiceLifetime.Scoped
                : (scope == InstanceLifetime.Singleton) ? ServiceLifetime.Singleton
                : ServiceLifetime.Transient;
            services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        }
    }
}

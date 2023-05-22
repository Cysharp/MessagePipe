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
        public static IMessagePipeBuilder AddRedis(this IMessagePipeBuilder builder, IConnectionMultiplexer connectionMultiplexer)
        {
            return AddRedis(builder, new SingleConnectionMultiplexerFactory(connectionMultiplexer), _ => { });
        }

        public static IMessagePipeBuilder AddRedis(this IMessagePipeBuilder builder, IConnectionMultiplexerFactory connectionMultiplexerFactory)
        {
            return AddRedis(builder, connectionMultiplexerFactory, _ => { });
        }

        public static IMessagePipeBuilder AddRedis<T>(this IMessagePipeBuilder builder)
            where T : class, IConnectionMultiplexerFactory
        {
            return AddRedis<T>(builder, _ => { });
        }

        public static IMessagePipeBuilder AddRedis<T>(this IMessagePipeBuilder builder, Action<MessagePipeRedisOptions> configure)
            where T : class, IConnectionMultiplexerFactory
        {
            return AddRedis(builder, ServiceDescriptor.Singleton<IConnectionMultiplexerFactory, T>(), configure);
        }

        public static IMessagePipeBuilder AddRedis(this IMessagePipeBuilder builder, IConnectionMultiplexer connectionMultiplexer, Action<MessagePipeRedisOptions> configure)
        {
            return AddRedis(builder, new SingleConnectionMultiplexerFactory(connectionMultiplexer), configure);
        }

        public static IMessagePipeBuilder AddRedis(this IMessagePipeBuilder builder, IConnectionMultiplexerFactory connectionMultiplexerFactory, Action<MessagePipeRedisOptions> configure)
        {
            return AddRedis(builder, ServiceDescriptor.Singleton(connectionMultiplexerFactory), configure);
        }

        static IMessagePipeBuilder AddRedis(IMessagePipeBuilder builder, ServiceDescriptor connectionMultiplexerServiceDesc, Action<MessagePipeRedisOptions> configure)
        {
            var options = new MessagePipeRedisOptions();
            configure(options);
            builder.Services.AddSingleton(options); // add as singleton instance
            builder.Services.Add(connectionMultiplexerServiceDesc);
            builder.Services.AddSingleton<IRedisSerializer>(options.RedisSerializer);

            builder.Services.Add(typeof(IDistributedPublisher<,>), typeof(RedisPublisher<,>), InstanceLifetime.Singleton);
            builder.Services.Add(typeof(IDistributedSubscriber<,>), typeof(RedisSubscriber<,>), InstanceLifetime.Singleton);

            return builder;
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

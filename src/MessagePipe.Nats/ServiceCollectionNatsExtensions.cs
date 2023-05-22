
using MessagePipe;
using MessagePipe.Nats;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionNatsExtensions
{
    public static IMessagePipeBuilder AddMessagePipeNats(this IMessagePipeBuilder builder, NatsConnectionFactory connectionFactory)
    {
        return AddMessagePipeNats(builder, connectionFactory, _ => { });
    }

    public static IMessagePipeBuilder AddMessagePipeNats(this IMessagePipeBuilder builder, NatsConnectionFactory connectionFactory, Action<MessagePipeNatsOptions> configure)
    {
        var options = new MessagePipeNatsOptions(connectionFactory);
        configure(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(options.NatsConnectionFactory);

        builder.Services.Add(typeof(IDistributedPublisher<,>), typeof(NatsPublisher<,>), InstanceLifetime.Singleton);
        builder.Services.Add(typeof(IDistributedSubscriber<,>), typeof(NatsSubscriber<,>), InstanceLifetime.Singleton);

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
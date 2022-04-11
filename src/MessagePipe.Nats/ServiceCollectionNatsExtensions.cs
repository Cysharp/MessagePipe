using Microsoft.Extensions.DependencyInjection;

namespace MessagePipe.Nats;

public static class ServiceCollectionNatsExtensions
{
    public static IServiceCollection AddMessagePipeNats(this IServiceCollection services, NatsConnectionFactory connectionFactory)
    {
        return AddMessagePipeNats(services, connectionFactory, _ => { });
    }

    public static IServiceCollection AddMessagePipeNats(this IServiceCollection services, NatsConnectionFactory connectionFactory, Action<MessagePipeNatsOptions> configure)
    {
        var options = new MessagePipeNatsOptions(connectionFactory);
        configure(options);
        services.AddSingleton(options);
        services.AddSingleton(options.NatsConnectionFactory);

        services.Add(typeof(IDistributedPublisher<,>), typeof(NatsPublisher<,>), InstanceLifetime.Singleton);
        services.Add(typeof(IDistributedSubscriber<,>), typeof(NatsSubscriber<,>), InstanceLifetime.Singleton);

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
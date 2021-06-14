using MessagePipe.InProcess;
using MessagePipe.InProcess.Workers;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MessagePipe
{
    public static class ServiceCollectionInProcessExtensions
    {
        public static IServiceCollection AddMessagePipeInProcessUdp(this IServiceCollection services, string host, int port)
        {
            return AddMessagePipeInProcessUdp(services, host, port, _ => { });
        }

        public static IServiceCollection AddMessagePipeInProcessUdp(this IServiceCollection services, string host, int port, Action<MessagePipeInProcessUdpOptions> configure)
        {
            var options = new MessagePipeInProcessUdpOptions(host, port);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(UdpWorker), options.InstanceLifetime);

            services.Add(typeof(IDistributedPublisher<,>), typeof(UdpDistributedPublisher<,>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(UdpDistributedSubscriber<,>), options.InstanceLifetime);

            return services;
        }

        public static IServiceCollection AddMessagePipeInProcessTcp(this IServiceCollection services, string host, int port)
        {
            return AddMessagePipeInProcessTcp(services, host, port, _ => { });
        }

        public static IServiceCollection AddMessagePipeInProcessTcp(this IServiceCollection services, string host, int port, Action<MessagePipeInProcessTcpOptions> configure)
        {
            var options = new MessagePipeInProcessTcpOptions(host, port);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(TcpWorker), options.InstanceLifetime);

            services.Add(typeof(IDistributedPublisher<,>), typeof(TcpDistributedPublisher<,>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(TcpDistributedSubscriber<,>), options.InstanceLifetime);
            services.Add(typeof(IRemoteRequestHandler<,>), typeof(TcpRemoteRequestHandler<,>), options.InstanceLifetime);

            return services;
        }

        public static IServiceCollection AddMessagePipeInProcessNamedPipe(this IServiceCollection services, string pipeName)
        {
            return AddMessagePipeInProcessNamedPipe(services, pipeName, _ => { });
        }

        public static IServiceCollection AddMessagePipeInProcessNamedPipe(this IServiceCollection services, string pipeName, Action<MessagePipeInProcessNamedPipeOptions> configure)
        {
            var options = new MessagePipeInProcessNamedPipeOptions(pipeName);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(NamedPipeWorker), options.InstanceLifetime);

            services.Add(typeof(IDistributedPublisher<,>), typeof(NamedPipeDistributedPublisher<,>), InstanceLifetime.Singleton);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(NamedPipeDistributedSubscriber<,>), InstanceLifetime.Singleton);
            services.Add(typeof(IRemoteRequestHandler<,>), typeof(NamedPipeRemoteRequestHandler<,>), options.InstanceLifetime);

            return services;
        }

        static void Add(this IServiceCollection services, Type serviceType, InstanceLifetime scope)
        {
            var lifetime = (scope == InstanceLifetime.Scoped) ? ServiceLifetime.Scoped
                : (scope == InstanceLifetime.Singleton) ? ServiceLifetime.Singleton
                : ServiceLifetime.Transient;
            services.Add(new ServiceDescriptor(serviceType, serviceType, lifetime));
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
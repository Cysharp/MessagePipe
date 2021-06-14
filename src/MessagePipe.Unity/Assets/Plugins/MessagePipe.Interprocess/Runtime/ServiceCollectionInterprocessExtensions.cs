#if !UNITY_2018_3_OR_NEWER

using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MessagePipe
{
    public static class ServiceCollectionInterprocessExtensions
    {
        public static IServiceCollection AddMessagePipeInterprocessUdp(this IServiceCollection services, string host, int port)
        {
            return AddMessagePipeInterprocessUdp(services, host, port, _ => { });
        }

        public static IServiceCollection AddMessagePipeInterprocessUdp(this IServiceCollection services, string host, int port, Action<MessagePipeInterprocessUdpOptions> configure)
        {
            var options = new MessagePipeInterprocessUdpOptions(host, port);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(UdpWorker), options.InstanceLifetime);

            services.Add(typeof(IDistributedPublisher<,>), typeof(UdpDistributedPublisher<,>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(UdpDistributedSubscriber<,>), options.InstanceLifetime);

            return services;
        }

        public static IServiceCollection AddMessagePipeInterprocessTcp(this IServiceCollection services, string host, int port)
        {
            return AddMessagePipeInterprocessTcp(services, host, port, _ => { });
        }

        public static IServiceCollection AddMessagePipeInterprocessTcp(this IServiceCollection services, string host, int port, Action<MessagePipeInterprocessTcpOptions> configure)
        {
            var options = new MessagePipeInterprocessTcpOptions(host, port);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(TcpWorker), options.InstanceLifetime);

            services.Add(typeof(IDistributedPublisher<,>), typeof(TcpDistributedPublisher<,>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(TcpDistributedSubscriber<,>), options.InstanceLifetime);
            services.Add(typeof(IRemoteRequestHandler<,>), typeof(TcpRemoteRequestHandler<,>), options.InstanceLifetime);

            return services;
        }

        public static IServiceCollection AddMessagePipeInterprocessNamedPipe(this IServiceCollection services, string pipeName)
        {
            return AddMessagePipeInterprocessNamedPipe(services, pipeName, _ => { });
        }

        public static IServiceCollection AddMessagePipeInterprocessNamedPipe(this IServiceCollection services, string pipeName, Action<MessagePipeInterprocessNamedPipeOptions> configure)
        {
            var options = new MessagePipeInterprocessNamedPipeOptions(pipeName);
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

#endif
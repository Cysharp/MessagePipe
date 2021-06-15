using System;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
#endif

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
            services.Add(serviceType, serviceType, scope);
        }

#if UNITY_2018_3_OR_NEWER

        public static IServiceCollection AddUpdInterprocessMessageBroker<TKey, TMessage>(this IServiceCollection services, MessagePipeOptions options)
        {
            services.Add(typeof(IDistributedPublisher<TKey, TMessage>), typeof(UdpDistributedPublisher<TKey, TMessage>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<TKey, TMessage>), typeof(UdpDistributedSubscriber<TKey, TMessage>), options.InstanceLifetime);

            return services;
        }

        public static IServiceCollection AddTcpInterprocessMessageBroker<TKey, TMessage>(this IServiceCollection services, MessagePipeOptions options)
        {
            services.Add(typeof(IDistributedPublisher<TKey, TMessage>), typeof(TcpDistributedPublisher<TKey, TMessage>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<TKey, TMessage>), typeof(TcpDistributedSubscriber<TKey, TMessage>), options.InstanceLifetime);

            return services;
        }

        public static IServiceCollection AddNamedPipeInterprocessMessageBroker<TKey, TMessage>(this IServiceCollection services, MessagePipeOptions options)
        {
            services.Add(typeof(IDistributedPublisher<TKey, TMessage>), typeof(NamedPipeDistributedPublisher<TKey, TMessage>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<TKey, TMessage>), typeof(NamedPipeDistributedSubscriber<TKey, TMessage>), options.InstanceLifetime);

            return services;
        }

        public static IServiceCollection AddTcpRemoteRequestHandler<TRequest, TResponse>(this IServiceCollection services, MessagePipeOptions options)
        {
            services.Add(typeof(IRemoteRequestHandler<TRequest, TResponse>), typeof(TcpRemoteRequestHandler<TRequest, TResponse>), options.InstanceLifetime);

            return services;
        }

        public static IServiceCollection AddNamedPipeRemoteRequestHandler<TRequest, TResponse>(this IServiceCollection services, MessagePipeOptions options)
        {
            services.Add(typeof(IRemoteRequestHandler<TRequest, TResponse>), typeof(NamedPipeRemoteRequestHandler<TRequest, TResponse>), options.InstanceLifetime);

            return services;
        }

#endif
    }
}
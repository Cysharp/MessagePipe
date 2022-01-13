using System;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
#endif

#if !UNITY_2018_3_OR_NEWER
using ReturnType = Microsoft.Extensions.DependencyInjection.IServiceCollection;
#else
using ReturnType = MessagePipe.Interprocess.MessagePipeInterprocessOptions;
#endif

namespace MessagePipe
{
    public static class ServiceCollectionInterprocessExtensions
    {
        public static ReturnType AddMessagePipeUdpInterprocess(this IServiceCollection services, string host, int port)
        {
            return AddMessagePipeUdpInterprocess(services, host, port, _ => { });
        }

        public static ReturnType AddMessagePipeUdpInterprocess(this IServiceCollection services, string host, int port, Action<MessagePipeInterprocessUdpOptions> configure)
        {
            var options = new MessagePipeInterprocessUdpOptions(host, port);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(UdpWorker), options.InstanceLifetime);

#if !UNITY_2018_3_OR_NEWER
            services.Add(typeof(IDistributedPublisher<,>), typeof(UdpDistributedPublisher<,>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(UdpDistributedSubscriber<,>), options.InstanceLifetime);
            return services;
#else
            AddAsyncMessageBroker<IInterprocessKey, IInterprocessValue>(services, options);
            return options;
#endif
        }

        public static ReturnType AddMessagePipeTcpInterprocess(this IServiceCollection services, string host, int port)
        {
            return AddMessagePipeTcpInterprocess(services, host, port, _ => { });
        }

        public static ReturnType AddMessagePipeTcpInterprocess(this IServiceCollection services, string host, int port, Action<MessagePipeInterprocessTcpOptions> configure)
        {
            var options = new MessagePipeInterprocessTcpOptions(host, port);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(TcpWorker), options.InstanceLifetime);

#if !UNITY_2018_3_OR_NEWER
            services.Add(typeof(IDistributedPublisher<,>), typeof(TcpDistributedPublisher<,>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(TcpDistributedSubscriber<,>), options.InstanceLifetime);
            services.Add(typeof(IRemoteRequestHandler<,>), typeof(TcpRemoteRequestHandler<,>), options.InstanceLifetime);
            return services;
#else
            AddAsyncMessageBroker<IInterprocessKey, IInterprocessValue>(services, options);
            return options;
#endif

        }

#if !UNITY_2018_3_OR_NEWER

        // NamedPipe in Unity is slightly buggy so disable.

        public static ReturnType AddMessagePipeNamedPipeInterprocess(this IServiceCollection services, string pipeName)
        {
            return AddMessagePipeNamedPipeInterprocess(services, pipeName, _ => { });
        }

        public static ReturnType AddMessagePipeNamedPipeInterprocess(this IServiceCollection services, string pipeName, Action<MessagePipeInterprocessNamedPipeOptions> configure)
        {
            var options = new MessagePipeInterprocessNamedPipeOptions(pipeName);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(NamedPipeWorker), options.InstanceLifetime);

#if !UNITY_2018_3_OR_NEWER
            services.Add(typeof(IDistributedPublisher<,>), typeof(NamedPipeDistributedPublisher<,>), InstanceLifetime.Singleton);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(NamedPipeDistributedSubscriber<,>), InstanceLifetime.Singleton);
            services.Add(typeof(IRemoteRequestHandler<,>), typeof(NamedPipeRemoteRequestHandler<,>), options.InstanceLifetime);
            return services;
#else
            AddAsyncMessageBroker<IInterprocessKey, IInterprocessValue>(services, options);
            return options;
#endif
        }

#endif

        static void Add(this IServiceCollection services, Type serviceType, InstanceLifetime scope)
        {
            services.Add(serviceType, serviceType, scope);
        }

#if !UNITY_2018_3_OR_NEWER

        static void Add(this IServiceCollection services, Type serviceType, Type implementationType, InstanceLifetime scope)
        {
            var lifetime = (scope == InstanceLifetime.Scoped) ? ServiceLifetime.Scoped
                : (scope == InstanceLifetime.Singleton) ? ServiceLifetime.Singleton
                : ServiceLifetime.Transient;

            var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);
            services.Add(descriptor);
        }

#endif

#if UNITY_2018_3_OR_NEWER

        static void AddAsyncMessageBroker<TKey,TMessage>(this IServiceCollection services, MessagePipeInterprocessOptions options)
        {
            services.Add(typeof(AsyncMessageBrokerCore<TKey, TMessage>), options.InstanceLifetime);
            services.Add(typeof(IAsyncPublisher<TKey, TMessage>), typeof(AsyncMessageBroker<TKey, TMessage>), options.InstanceLifetime);
            services.Add(typeof(IAsyncSubscriber<TKey, TMessage>), typeof(AsyncMessageBroker<TKey, TMessage>), options.InstanceLifetime);
        }

        public static IServiceCollection RegisterUpdInterprocessMessageBroker<TKey, TMessage>(this IServiceCollection services, MessagePipeInterprocessOptions options)
        {
            AddAsyncMessageBroker<TKey, TMessage>(services, options);
            services.Add(typeof(IDistributedPublisher<TKey, TMessage>), typeof(UdpDistributedPublisher<TKey, TMessage>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<TKey, TMessage>), typeof(UdpDistributedSubscriber<TKey, TMessage>), options.InstanceLifetime);

            return services;
        }

        public static IServiceCollection RegisterTcpInterprocessMessageBroker<TKey, TMessage>(this IServiceCollection services, MessagePipeInterprocessOptions options)
        {
            AddAsyncMessageBroker<TKey, TMessage>(services, options);
            services.Add(typeof(IDistributedPublisher<TKey, TMessage>), typeof(TcpDistributedPublisher<TKey, TMessage>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<TKey, TMessage>), typeof(TcpDistributedSubscriber<TKey, TMessage>), options.InstanceLifetime);

            return services;
        }

        //public static IServiceCollection RegisterNamedPipeInterprocessMessageBroker<TKey, TMessage>(this IServiceCollection services, MessagePipeInterprocessOptions options)
        //{
        //    AddAsyncMessageBroker<TKey, TMessage>(services, options);
        //    services.Add(typeof(IDistributedPublisher<TKey, TMessage>), typeof(NamedPipeDistributedPublisher<TKey, TMessage>), options.InstanceLifetime);
        //    services.Add(typeof(IDistributedSubscriber<TKey, TMessage>), typeof(NamedPipeDistributedSubscriber<TKey, TMessage>), options.InstanceLifetime);

        //    return services;
        //}

        public static IServiceCollection RegisterTcpRemoteRequestHandler<TRequest, TResponse>(this IServiceCollection services, MessagePipeInterprocessOptions options)
        {
            services.Add(typeof(IRemoteRequestHandler<TRequest, TResponse>), typeof(TcpRemoteRequestHandler<TRequest, TResponse>), options.InstanceLifetime);

            return services;
        }

        //public static IServiceCollection RegisterNamedPipeRemoteRequestHandler<TRequest, TResponse>(this IServiceCollection services, MessagePipeInterprocessOptions options)
        //{
        //    services.Add(typeof(IRemoteRequestHandler<TRequest, TResponse>), typeof(NamedPipeRemoteRequestHandler<TRequest, TResponse>), options.InstanceLifetime);

        //    return services;
        //}

#endif
#if NET5_0_OR_GREATER
        public static ReturnType AddMessagePipeUdpInterprocessUds(this IServiceCollection services, string domainSocketPath)
        {
            return AddMessagePipeUdpInterprocessUds(services, domainSocketPath, _ => { });
        }
        public static ReturnType AddMessagePipeUdpInterprocessUds(this IServiceCollection services, string domainSocketPath, Action<MessagePipeInterprocessUdpUdsOptions> configure)
        {
            var options = new MessagePipeInterprocessUdpUdsOptions(domainSocketPath);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(UdpWorker), options.InstanceLifetime);

#if !UNITY_2018_3_OR_NEWER
            services.Add(typeof(IDistributedPublisher<,>), typeof(UdpDistributedPublisher<,>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(UdpDistributedSubscriber<,>), options.InstanceLifetime);
            return services;
#else
            AddAsyncMessageBroker<IInterprocessKey, IInterprocessValue>(services, options);
            return options;
#endif
        }
        public static ReturnType AddMessagePipeTcpInterprocessUds(this IServiceCollection services, string domainSocketPath)
        {
            return AddMessagePipeTcpInterprocessUds(services, domainSocketPath, _ => { });
        }

        public static ReturnType AddMessagePipeTcpInterprocessUds(this IServiceCollection services, string domainSocketPath, Action<MessagePipeInterprocessTcpUdsOptions> configure)
        {
            var options = new MessagePipeInterprocessTcpUdsOptions(domainSocketPath);
            configure(options);

            services.AddSingleton(options);
            services.Add(typeof(TcpWorker), options.InstanceLifetime);

#if !UNITY_2018_3_OR_NEWER
            services.Add(typeof(IDistributedPublisher<,>), typeof(TcpDistributedPublisher<,>), options.InstanceLifetime);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(TcpDistributedSubscriber<,>), options.InstanceLifetime);
            services.Add(typeof(IRemoteRequestHandler<,>), typeof(TcpRemoteRequestHandler<,>), options.InstanceLifetime);
            return services;
#else
            AddAsyncMessageBroker<IInterprocessKey, IInterprocessValue>(services, options);
            return options;
#endif

        }
#endif // NET5_0_OR_GREATER
    }
}
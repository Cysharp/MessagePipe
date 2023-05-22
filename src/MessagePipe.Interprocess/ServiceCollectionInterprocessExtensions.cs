using System;
using MessagePipe;
using MessagePipe.Interprocess;
using MessagePipe.Interprocess.Workers;
#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
#endif

#if !UNITY_2018_3_OR_NEWER
using ReturnType = Microsoft.Extensions.DependencyInjection.IMessagePipeBuilder;
namespace Microsoft.Extensions.DependencyInjection
#else
using ReturnType = MessagePipe.Interprocess.MessagePipeInterprocessOptions;
namespace MessagePipe
#endif
{
    public static class ServiceCollectionInterprocessExtensions
    {
        public static ReturnType AddUdpInterprocess(this IMessagePipeBuilder builder, string host, int port)
        {
            return AddUdpInterprocess(builder, host, port, _ => { });
        }

        public static ReturnType AddUdpInterprocess(this IMessagePipeBuilder builder, string host, int port, Action<MessagePipeInterprocessUdpOptions> configure)
        {
            var options = new MessagePipeInterprocessUdpOptions(host, port);
            configure(options);

            builder.Services.AddSingleton(options);
            builder.Services.Add(typeof(UdpWorker), options.InstanceLifetime);

#if !UNITY_2018_3_OR_NEWER
            builder.Services.Add(typeof(IDistributedPublisher<,>), typeof(UdpDistributedPublisher<,>), options.InstanceLifetime);
            builder.Services.Add(typeof(IDistributedSubscriber<,>), typeof(UdpDistributedSubscriber<,>), options.InstanceLifetime);
            return builder;
#else
            AddAsyncMessageBroker<IInterprocessKey, IInterprocessValue>(builder, options);
            return options;
#endif
        }

        public static ReturnType AddTcpInterprocess(this IMessagePipeBuilder builder, string host, int port)
        {
            return AddTcpInterprocess(builder, host, port, _ => { });
        }

        public static ReturnType AddTcpInterprocess(this IMessagePipeBuilder builder, string host, int port, Action<MessagePipeInterprocessTcpOptions> configure)
        {
            var options = new MessagePipeInterprocessTcpOptions(host, port);
            configure(options);

            builder.Services.AddSingleton(options);
            builder.Services.Add(typeof(TcpWorker), options.InstanceLifetime);

#if !UNITY_2018_3_OR_NEWER
            builder.Services.Add(typeof(IDistributedPublisher<,>), typeof(TcpDistributedPublisher<,>), options.InstanceLifetime);
            builder.Services.Add(typeof(IDistributedSubscriber<,>), typeof(TcpDistributedSubscriber<,>), options.InstanceLifetime);
            builder.Services.Add(typeof(IRemoteRequestHandler<,>), typeof(TcpRemoteRequestHandler<,>), options.InstanceLifetime);
            return builder;
#else
            AddAsyncMessageBroker<IInterprocessKey, IInterprocessValue>(builder, options);
            return options;
#endif

        }

#if !UNITY_2018_3_OR_NEWER

        // NamedPipe in Unity is slightly buggy so disable.

        public static ReturnType AddNamedPipeInterprocess(this IMessagePipeBuilder builder, string pipeName)
        {
            return AddNamedPipeInterprocess(builder, pipeName, _ => { });
        }

        public static ReturnType AddNamedPipeInterprocess(this IMessagePipeBuilder builder, string pipeName, Action<MessagePipeInterprocessNamedPipeOptions> configure)
        {
            var options = new MessagePipeInterprocessNamedPipeOptions(pipeName);
            configure(options);

            builder.Services.AddSingleton(options);
            builder.Services.Add(typeof(NamedPipeWorker), options.InstanceLifetime);

#if !UNITY_2018_3_OR_NEWER
            builder.Services.Add(typeof(IDistributedPublisher<,>), typeof(NamedPipeDistributedPublisher<,>), InstanceLifetime.Singleton);
            builder.Services.Add(typeof(IDistributedSubscriber<,>), typeof(NamedPipeDistributedSubscriber<,>), InstanceLifetime.Singleton);
            builder.Services.Add(typeof(IRemoteRequestHandler<,>), typeof(NamedPipeRemoteRequestHandler<,>), options.InstanceLifetime);
            return builder;
#else
            AddAsyncMessageBroker<IInterprocessKey, IInterprocessValue>(builder, options);
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

        static void AddAsyncMessageBroker<TKey,TMessage>(this IMessagePipeBuilder builder, MessagePipeInterprocessOptions options)
        {
            builder.Services.Add(typeof(AsyncMessageBrokerCore<TKey, TMessage>), options.InstanceLifetime);
            builder.Services.Add(typeof(IAsyncPublisher<TKey, TMessage>), typeof(AsyncMessageBroker<TKey, TMessage>), options.InstanceLifetime);
            builder.Services.Add(typeof(IAsyncSubscriber<TKey, TMessage>), typeof(AsyncMessageBroker<TKey, TMessage>), options.InstanceLifetime);
        }

        public static IMessagePipeBuilder RegisterUpdInterprocessMessageBroker<TKey, TMessage>(this IMessagePipeBuilder builder, MessagePipeInterprocessOptions options)
        {
            AddAsyncMessageBroker<TKey, TMessage>(builder, options);
            builder.Services.Add(typeof(IDistributedPublisher<TKey, TMessage>), typeof(UdpDistributedPublisher<TKey, TMessage>), options.InstanceLifetime);
            builder.Services.Add(typeof(IDistributedSubscriber<TKey, TMessage>), typeof(UdpDistributedSubscriber<TKey, TMessage>), options.InstanceLifetime);

            return builder;
        }

        public static IMessagePipeBuilder RegisterTcpInterprocessMessageBroker<TKey, TMessage>(this IMessagePipeBuilder builder, MessagePipeInterprocessOptions options)
        {
            AddAsyncMessageBroker<TKey, TMessage>(builder, options);
            builder.Services.Add(typeof(IDistributedPublisher<TKey, TMessage>), typeof(TcpDistributedPublisher<TKey, TMessage>), options.InstanceLifetime);
            builder.Services.Add(typeof(IDistributedSubscriber<TKey, TMessage>), typeof(TcpDistributedSubscriber<TKey, TMessage>), options.InstanceLifetime);

            return builder;
        }

        //public static IMessagePipeBuilder RegisterNamedPipeInterprocessMessageBroker<TKey, TMessage>(this IMessagePipeBuilder builder, MessagePipeInterprocessOptions options)
        //{
        //    AddAsyncMessageBroker<TKey, TMessage>(builder, options);
        //    builder.Services.Add(typeof(IDistributedPublisher<TKey, TMessage>), typeof(NamedPipeDistributedPublisher<TKey, TMessage>), options.InstanceLifetime);
        //    builder.Services.Add(typeof(IDistributedSubscriber<TKey, TMessage>), typeof(NamedPipeDistributedSubscriber<TKey, TMessage>), options.InstanceLifetime);

        //    return builder;
        //}

        public static IMessagePipeBuilder RegisterTcpRemoteRequestHandler<TRequest, TResponse>(this IMessagePipeBuilder builder, MessagePipeInterprocessOptions options)
        {
            builder.Services.Add(typeof(IRemoteRequestHandler<TRequest, TResponse>), typeof(TcpRemoteRequestHandler<TRequest, TResponse>), options.InstanceLifetime);

            return builder;
        }

        //public static IMessagePipeBuilder RegisterNamedPipeRemoteRequestHandler<TRequest, TResponse>(this IMessagePipeBuilder builder, MessagePipeInterprocessOptions options)
        //{
        //    builder.Services.Add(typeof(IRemoteRequestHandler<TRequest, TResponse>), typeof(NamedPipeRemoteRequestHandler<TRequest, TResponse>), options.InstanceLifetime);

        //    return builder;
        //}

#endif
#if NET5_0_OR_GREATER
        public static ReturnType AddUdpInterprocessUds(this IMessagePipeBuilder builder, string domainSocketPath)
        {
            return AddUdpInterprocessUds(builder, domainSocketPath, _ => { });
        }
        public static ReturnType AddUdpInterprocessUds(this IMessagePipeBuilder builder, string domainSocketPath, Action<MessagePipeInterprocessUdpUdsOptions> configure)
        {
            var options = new MessagePipeInterprocessUdpUdsOptions(domainSocketPath);
            configure(options);

            builder.Services.AddSingleton(options);
            builder.Services.Add(typeof(UdpWorker), options.InstanceLifetime);

#if !UNITY_2018_3_OR_NEWER
            builder.Services.Add(typeof(IDistributedPublisher<,>), typeof(UdpDistributedPublisher<,>), options.InstanceLifetime);
            builder.Services.Add(typeof(IDistributedSubscriber<,>), typeof(UdpDistributedSubscriber<,>), options.InstanceLifetime);
            return builder;
#else
            AddAsyncMessageBroker<IInterprocessKey, IInterprocessValue>(builder, options);
            return options;
#endif
        }
        public static ReturnType AddTcpInterprocessUds(this IMessagePipeBuilder builder, string domainSocketPath)
        {
            return AddTcpInterprocessUds(builder, domainSocketPath, _ => { });
        }

        public static ReturnType AddTcpInterprocessUds(this IMessagePipeBuilder builder, string domainSocketPath, Action<MessagePipeInterprocessTcpUdsOptions> configure)
        {
            var options = new MessagePipeInterprocessTcpUdsOptions(domainSocketPath);
            configure(options);

            builder.Services.AddSingleton(options);
            builder.Services.Add(typeof(TcpWorker), options.InstanceLifetime);

#if !UNITY_2018_3_OR_NEWER
            builder.Services.Add(typeof(IDistributedPublisher<,>), typeof(TcpDistributedPublisher<,>), options.InstanceLifetime);
            builder.Services.Add(typeof(IDistributedSubscriber<,>), typeof(TcpDistributedSubscriber<,>), options.InstanceLifetime);
            builder.Services.Add(typeof(IRemoteRequestHandler<,>), typeof(TcpRemoteRequestHandler<,>), options.InstanceLifetime);
            return builder;
#else
            AddAsyncMessageBroker<IInterprocessKey, IInterprocessValue>(builder, options);
            return options;
#endif

        }
#endif // NET5_0_OR_GREATER
    }
}
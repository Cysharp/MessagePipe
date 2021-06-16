using System;
using VContainer;
using MessagePipe.VContainer;

namespace MessagePipe
{
    public static class ContainerBuilderExtensions
    {
        // original is ServiceCollectionExtensions, trimed openegenerics register.

        public static MessagePipeOptions RegisterMessagePipe(this IContainerBuilder builder)
        {
            return RegisterMessagePipe(builder, _ => { });
        }

        public static MessagePipeOptions RegisterMessagePipe(this IContainerBuilder builder, Action<MessagePipeOptions> configure)
        {
            MessagePipeOptions options = null;
            var proxy = new ContainerBuilderProxy(builder);
            proxy.AddMessagePipe(x =>
             {
                 configure(x);
                 options = x;
             });

            builder.Register<IServiceProvider, ObjectResolverProxy>(Lifetime.Scoped);

            return options;
        }

        /// <summary>Register IPublisher[TMessage] and ISubscriber[TMessage](includes Async/Buffered) to container builder.</summary>
        public static IContainerBuilder RegisterMessageBroker<TMessage>(this IContainerBuilder builder, MessagePipeOptions options)
        {
            var lifetime = GetLifetime(options.InstanceLifetime);
            var services = new ContainerBuilderProxy(builder);

            // keyless PubSub
            services.Add(typeof(MessageBrokerCore<TMessage>), lifetime);
            services.Add(typeof(IPublisher<TMessage>), typeof(MessageBroker<TMessage>), lifetime);
            services.Add(typeof(ISubscriber<TMessage>), typeof(MessageBroker<TMessage>), lifetime);

            // keyless PubSub async
            services.Add(typeof(AsyncMessageBrokerCore<TMessage>), lifetime);
            services.Add(typeof(IAsyncPublisher<TMessage>), typeof(AsyncMessageBroker<TMessage>), lifetime);
            services.Add(typeof(IAsyncSubscriber<TMessage>), typeof(AsyncMessageBroker<TMessage>), lifetime);

            // keyless buffered PubSub
            services.Add(typeof(BufferedMessageBrokerCore<TMessage>), lifetime);
            services.Add(typeof(IBufferedPublisher<TMessage>), typeof(BufferedMessageBroker<TMessage>), lifetime);
            services.Add(typeof(IBufferedSubscriber<TMessage>), typeof(BufferedMessageBroker<TMessage>), lifetime);

            // keyless buffered PubSub async
            services.Add(typeof(BufferedAsyncMessageBrokerCore<TMessage>), lifetime);
            services.Add(typeof(IBufferedAsyncPublisher<TMessage>), typeof(BufferedAsyncMessageBroker<TMessage>), lifetime);
            services.Add(typeof(IBufferedAsyncSubscriber<TMessage>), typeof(BufferedAsyncMessageBroker<TMessage>), lifetime);

            return builder;
        }

        /// <summary>Register IPublisher[TKey, TMessage] and ISubscriber[TKey, TMessage](includes Async) to container builder.</summary>
        public static IContainerBuilder RegisterMessageBroker<TKey, TMessage>(this IContainerBuilder builder, MessagePipeOptions options)
        {
            var lifetime = GetLifetime(options.InstanceLifetime);
            var services = new ContainerBuilderProxy(builder);

            // keyed PubSub
            services.Add(typeof(MessageBrokerCore<TKey, TMessage>), lifetime);
            services.Add(typeof(IPublisher<TKey, TMessage>), typeof(MessageBroker<TKey, TMessage>), lifetime);
            services.Add(typeof(ISubscriber<TKey, TMessage>), typeof(MessageBroker<TKey, TMessage>), lifetime);

            // keyed PubSub async
            services.Add(typeof(AsyncMessageBrokerCore<TKey, TMessage>), lifetime);
            services.Add(typeof(IAsyncPublisher<TKey, TMessage>), typeof(AsyncMessageBroker<TKey, TMessage>), lifetime);
            services.Add(typeof(IAsyncSubscriber<TKey, TMessage>), typeof(AsyncMessageBroker<TKey, TMessage>), lifetime);

            return builder;
        }

        /// <summary>Register IRequestHandler[TRequest, TResponse](includes All) to container builder.</summary>
        public static IContainerBuilder RegisterRequestHandler<TRequest, TResponse, THandler>(this IContainerBuilder builder, MessagePipeOptions options)
            where THandler : IRequestHandler
        {
            var lifetime = GetLifetime(options.RequestHandlerLifetime);
            var services = new ContainerBuilderProxy(builder);

            services.Add(typeof(IRequestHandlerCore<TRequest, TResponse>), typeof(THandler), lifetime);
            if (!builder.Exists(typeof(IRequestHandler<TRequest, TResponse>), true))
            {
                services.Add(typeof(IRequestHandler<TRequest, TResponse>), typeof(RequestHandler<TRequest, TResponse>), lifetime);
                services.Add(typeof(IRequestAllHandler<TRequest, TResponse>), typeof(RequestAllHandler<TRequest, TResponse>), lifetime);
            }

            return builder;
        }

        /// <summary>Register IAsyncRequestHandler[TRequest, TResponse](includes All) to container builder.</summary>
        public static IContainerBuilder RegisterAsyncRequestHandler<TRequest, TResponse, THandler>(this IContainerBuilder builder, MessagePipeOptions options)
            where THandler : IAsyncRequestHandler
        {
            var lifetime = GetLifetime(options.RequestHandlerLifetime);
            var services = new ContainerBuilderProxy(builder);

            services.Add(typeof(IAsyncRequestHandlerCore<TRequest, TResponse>), typeof(THandler), lifetime);
            if (!builder.Exists(typeof(IAsyncRequestHandler<TRequest, TResponse>), true))
            {
                services.Add(typeof(IAsyncRequestHandler<TRequest, TResponse>), typeof(AsyncRequestHandler<TRequest, TResponse>), lifetime);
                services.Add(typeof(IAsyncRequestAllHandler<TRequest, TResponse>), typeof(AsyncRequestAllHandler<TRequest, TResponse>), lifetime);
            }

            AsyncRequestHandlerRegistory.Add(typeof(TRequest), typeof(TResponse), typeof(THandler));
            return builder;
        }

        public static IContainerBuilder RegisterMessageHandlerFilter<T>(this IContainerBuilder builder)
            where T : class, IMessageHandlerFilter
        {
            if (!builder.Exists(typeof(T), true))
            {
                builder.Register<T>(Lifetime.Transient);
            }
            return builder;
        }

        public static IContainerBuilder RegisterAsyncMessageHandlerFilter<T>(this IContainerBuilder builder)
            where T : class, IAsyncMessageHandlerFilter
        {
            if (!builder.Exists(typeof(T), true))
            {
                builder.Register<T>(Lifetime.Transient);
            }
            return builder;
        }

        public static IContainerBuilder RegisterRequestHandlerFilter<T>(this IContainerBuilder builder)
            where T : class, IRequestHandlerFilter
        {
            if (!builder.Exists(typeof(T), true))
            {
                builder.Register<T>(Lifetime.Transient);
            }
            return builder;
        }

        public static IContainerBuilder RegisterAsyncRequestHandlerFilter<T>(this IContainerBuilder builder)
            where T : class, IAsyncRequestHandlerFilter
        {
            if (!builder.Exists(typeof(T), true))
            {
                builder.Register<T>(Lifetime.Transient);
            }

            return builder;
        }

        public static IServiceCollection AsServiceCollection(this IContainerBuilder builder)
        {
            return new ContainerBuilderProxy(builder);
        }

        static Lifetime GetLifetime(InstanceLifetime lifetime)
        {
            return (lifetime == InstanceLifetime.Scoped) ? Lifetime.Scoped
                : (lifetime == InstanceLifetime.Singleton) ? Lifetime.Singleton
                : Lifetime.Transient;
        }
    }
}
using System;
using Zenject;
using MessagePipe.Zenject;

namespace MessagePipe
{
    public static partial class DiContainerExtensions
    {
        // original is ServiceCollectionExtensions, trimed openegenerics register.

        public static MessagePipeOptions BindMessagePipe(this DiContainer builder)
        {
            return BindMessagePipe(builder, _ => { });
        }

        public static MessagePipeOptions BindMessagePipe(this DiContainer builder, Action<MessagePipeOptions> configure)
        {
            MessagePipeOptions options = null;
            var proxy = new DiContainerProxy(builder);
            proxy.AddMessagePipe(x =>
            {
                configure(x);
                options = x;

                // Zenject 6 does not allow regsiter multiple singleton, it causes annoying error.
                // https://github.com/modesttree/Zenject#upgrade-guide-for-zenject-6
                // so force use Scoped.
                options.InstanceLifetime = (options.InstanceLifetime == InstanceLifetime.Singleton)
                    ? InstanceLifetime.Scoped
                    : options.InstanceLifetime;
                options.RequestHandlerLifetime = (options.RequestHandlerLifetime == InstanceLifetime.Singleton)
                    ? InstanceLifetime.Scoped
                    : options.RequestHandlerLifetime;
            });

            builder.Bind<IServiceProvider>().To<DiContainerProviderProxy>().AsCached();

            return options;
        }

        /// <summary>Register IPublisher[TMessage] and ISubscriber[TMessage](includes Async/Buffered) to container builder.</summary>
        public static DiContainer BindMessageBroker<TMessage>(this DiContainer builder, MessagePipeOptions options)
        {
            var lifetime = options.InstanceLifetime;
            var services = new DiContainerProxy(builder);

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
        public static DiContainer BindMessageBroker<TKey, TMessage>(this DiContainer builder, MessagePipeOptions options)
        {
            var lifetime = options.InstanceLifetime;
            var services = new DiContainerProxy(builder);

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
        public static DiContainer BindRequestHandler<TRequest, TResponse, THandler>(this DiContainer builder, MessagePipeOptions options)
            where THandler : IRequestHandler
        {
            var lifetime = options.RequestHandlerLifetime;
            var services = new DiContainerProxy(builder);

            services.Add(typeof(IRequestHandlerCore<TRequest, TResponse>), typeof(THandler), lifetime);
            if (!builder.HasBinding<IRequestHandler<TRequest, TResponse>>())
            {
                services.Add(typeof(IRequestHandler<TRequest, TResponse>), typeof(RequestHandler<TRequest, TResponse>), lifetime);
                services.Add(typeof(IRequestAllHandler<TRequest, TResponse>), typeof(RequestAllHandler<TRequest, TResponse>), lifetime);
            }
            return builder;
        }

        /// <summary>Register IAsyncRequestHandler[TRequest, TResponse](includes All) to container builder.</summary>
        public static DiContainer BindAsyncRequestHandler<TRequest, TResponse, THandler>(this DiContainer builder, MessagePipeOptions options)
            where THandler : IAsyncRequestHandler
        {
            var lifetime = options.RequestHandlerLifetime;
            var services = new DiContainerProxy(builder);

            services.Add(typeof(IAsyncRequestHandlerCore<TRequest, TResponse>), typeof(THandler), lifetime);
            if (!builder.HasBinding<IAsyncRequestHandler<TRequest, TResponse>>())
            {
                services.Add(typeof(IAsyncRequestHandler<TRequest, TResponse>), typeof(AsyncRequestHandler<TRequest, TResponse>), lifetime);
                services.Add(typeof(IAsyncRequestAllHandler<TRequest, TResponse>), typeof(AsyncRequestAllHandler<TRequest, TResponse>), lifetime);
            }

            AsyncRequestHandlerRegistory.Add(typeof(TRequest), typeof(TResponse), typeof(THandler));
            return builder;
        }

        public static DiContainer BindMessageHandlerFilter<T>(this DiContainer builder)
            where T : class, IMessageHandlerFilter
        {
            if (!builder.HasBinding<T>())
            {
                builder.Bind<T>().AsTransient();
            }
            return builder;
        }

        public static DiContainer BindAsyncMessageHandlerFilter<T>(this DiContainer builder)
            where T : class, IAsyncMessageHandlerFilter
        {
            if (!builder.HasBinding<T>())
            {
                builder.Bind<T>().AsTransient();
            }
            return builder;
        }

        public static DiContainer BindRequestHandlerFilter<T>(this DiContainer builder)
            where T : class, IRequestHandlerFilter
        {
            if (!builder.HasBinding<T>())
            {
                builder.Bind<T>().AsTransient();
            }
            return builder;
        }

        public static DiContainer BindAsyncRequestHandlerFilter<T>(this DiContainer builder)
            where T : class, IAsyncRequestHandlerFilter
        {
            if (!builder.HasBinding<T>())
            {
                builder.Bind<T>().AsTransient();
            }
            return builder;
        }
    }
}
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
            });

            builder.Bind<IServiceProvider>().To<DiContainerProviderProxy>().AsCached();

            return options;
        }

        /// <summary>Register IPublisher[TMessage] and ISubscriber[TMessage](includes Async/Buffered) to container builder.</summary>
        public static DiContainer BindMessageBroker<TMessage>(this DiContainer builder, MessagePipeOptions options, ZenjectScope scope = ZenjectScope.Single)
        {
            var services = new DiContainerProxy(builder);

            // keyless PubSub
            services.Add(typeof(MessageBrokerCore<TMessage>), scope);
            services.Add(typeof(IPublisher<TMessage>), typeof(MessageBroker<TMessage>), scope);
            services.Add(typeof(ISubscriber<TMessage>), typeof(MessageBroker<TMessage>), scope);

            // keyless PubSub async
            services.Add(typeof(AsyncMessageBrokerCore<TMessage>), scope);
            services.Add(typeof(IAsyncPublisher<TMessage>), typeof(AsyncMessageBroker<TMessage>), scope);
            services.Add(typeof(IAsyncSubscriber<TMessage>), typeof(AsyncMessageBroker<TMessage>), scope);

            // keyless buffered PubSub
            services.Add(typeof(BufferedMessageBrokerCore<TMessage>), scope);
            services.Add(typeof(IBufferedPublisher<TMessage>), typeof(BufferedMessageBroker<TMessage>), scope);
            services.Add(typeof(IBufferedSubscriber<TMessage>), typeof(BufferedMessageBroker<TMessage>), scope);

            // keyless buffered PubSub async
            services.Add(typeof(BufferedAsyncMessageBrokerCore<TMessage>), scope);
            services.Add(typeof(IBufferedAsyncPublisher<TMessage>), typeof(BufferedAsyncMessageBroker<TMessage>), scope);
            services.Add(typeof(IBufferedAsyncSubscriber<TMessage>), typeof(BufferedAsyncMessageBroker<TMessage>), scope);

            return builder;
        }

        /// <summary>Register IPublisher[TKey, TMessage] and ISubscriber[TKey, TMessage](includes Async) to container builder.</summary>
        public static DiContainer BindMessageBroker<TKey, TMessage>(this DiContainer builder, MessagePipeOptions options, ZenjectScope scope = ZenjectScope.Single)
        {
            var services = new DiContainerProxy(builder);

            // keyed PubSub
            services.Add(typeof(MessageBrokerCore<TKey, TMessage>), scope);
            services.Add(typeof(IPublisher<TKey, TMessage>), typeof(MessageBroker<TKey, TMessage>), scope);
            services.Add(typeof(ISubscriber<TKey, TMessage>), typeof(MessageBroker<TKey, TMessage>), scope);

            // keyed PubSub async
            services.Add(typeof(AsyncMessageBrokerCore<TKey, TMessage>), scope);
            services.Add(typeof(IAsyncPublisher<TKey, TMessage>), typeof(AsyncMessageBroker<TKey, TMessage>), scope);
            services.Add(typeof(IAsyncSubscriber<TKey, TMessage>), typeof(AsyncMessageBroker<TKey, TMessage>), scope);

            return builder;
        }

        /// <summary>Register IRequestHandler[TRequest, TResponse](includes All) to container builder.</summary>
        public static DiContainer BindRequestHandler<TRequest, TResponse, THandler>(this DiContainer builder, MessagePipeOptions options, ZenjectScope scope = ZenjectScope.Single)
            where THandler : IRequestHandler
        {
            var services = new DiContainerProxy(builder);

            services.Add(typeof(IRequestHandlerCore<TRequest, TResponse>), typeof(THandler), scope);
            if (!builder.HasBinding<IRequestHandler<TRequest, TResponse>>())
            {
                services.Add(typeof(IRequestHandler<TRequest, TResponse>), typeof(RequestHandler<TRequest, TResponse>), scope);
                services.Add(typeof(IRequestAllHandler<TRequest, TResponse>), typeof(RequestAllHandler<TRequest, TResponse>), scope);
            }
            return builder;
        }

        /// <summary>Register IAsyncRequestHandler[TRequest, TResponse](includes All) to container builder.</summary>
        public static DiContainer BindAsyncRequestHandler<TRequest, TResponse, THandler>(this DiContainer builder, MessagePipeOptions options, ZenjectScope scope = ZenjectScope.Single)
            where THandler : IAsyncRequestHandler
        {
            var services = new DiContainerProxy(builder);

            services.Add(typeof(IAsyncRequestHandlerCore<TRequest, TResponse>), typeof(THandler), scope);
            if (!builder.HasBinding<IAsyncRequestHandler<TRequest, TResponse>>())
            {
                services.Add(typeof(IAsyncRequestHandler<TRequest, TResponse>), typeof(AsyncRequestHandler<TRequest, TResponse>), scope);
                services.Add(typeof(IAsyncRequestAllHandler<TRequest, TResponse>), typeof(AsyncRequestAllHandler<TRequest, TResponse>), scope);
            }
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
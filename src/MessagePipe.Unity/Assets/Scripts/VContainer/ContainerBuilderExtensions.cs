using System;
using VContainer;

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
            var options = new MessagePipeOptions();
            configure(options);

            builder.RegisterInstance(options);
            builder.Register<IServiceProvider, ObjectResolverProxy>(Lifetime.Scoped);
            builder.Register<MessagePipeDiagnosticsInfo>(Lifetime.Singleton);
            builder.Register<AttributeFilterProvider<MessageHandlerFilterAttribute>>(Lifetime.Singleton);
            builder.Register<FilterAttachedMessageHandlerFactory>(Lifetime.Singleton);
            builder.Register<FilterAttachedAsyncMessageHandlerFactory>(Lifetime.Singleton);

            return options;
        }

        /// <summary>Register IPublisher[TMessage] and ISubscriber[TMessage] to container builder.</summary>
        public static void RegisterMessageBroker<TMessage>(this IContainerBuilder builder, MessagePipeOptions options)
        {
            var lifetime = GetLifetime(options);

            builder.Register<MessageBrokerCore<TMessage>>(lifetime);
            builder.Register<IPublisher<TMessage>, MessageBroker<TMessage>>(lifetime);
            builder.Register<ISubscriber<TMessage>, MessageBroker<TMessage>>(lifetime);

            builder.Register<AsyncMessageBrokerCore<TMessage>>(lifetime);
            builder.Register<IAsyncPublisher<TMessage>, AsyncMessageBroker<TMessage>>(lifetime);
            builder.Register<IAsyncSubscriber<TMessage>, AsyncMessageBroker<TMessage>>(lifetime);
        }

        /// <summary>Register IPublisher[TKey, TMessage] and ISubscriber[TKey, TMessage] to container builder.</summary>
        public static void RegisterMessageBroker<TKey, TMessage>(this IContainerBuilder builder, MessagePipeOptions options)
        {
            var lifetime = GetLifetime(options);

            builder.Register<MessageBrokerCore<TKey, TMessage>>(lifetime);
            builder.Register<IPublisher<TKey, TMessage>, MessageBroker<TKey, TMessage>>(lifetime);
            builder.Register<ISubscriber<TKey, TMessage>, MessageBroker<TKey, TMessage>>(lifetime);

            builder.Register<AsyncMessageBrokerCore<TKey, TMessage>>(lifetime);
            builder.Register<IAsyncPublisher<TKey, TMessage>, AsyncMessageBroker<TKey, TMessage>>(lifetime);
            builder.Register<IAsyncSubscriber<TKey, TMessage>, AsyncMessageBroker<TKey, TMessage>>(lifetime);
        }

        static Lifetime GetLifetime(MessagePipeOptions options)
        {
            return options.InstanceLifetime == InstanceLifetime.Scoped ? Lifetime.Scoped : Lifetime.Singleton;
        }
    }
}




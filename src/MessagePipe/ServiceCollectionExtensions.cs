using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMessagePipe(this IServiceCollection services)
        {
            return AddMessagePipe(services, _ => { });
        }

        public static IServiceCollection AddMessagePipe(this IServiceCollection services, Action<MessagePipeOptions> configure)
        {
            var options = new MessagePipeOptions();
            configure(options);
            services.AddSingleton(options);

            // keyed PubSub
            services.AddSingleton(typeof(IMessageBroker<,>), typeof(ImmutableArrayMessageBroker<,>));
            services.AddSingleton(typeof(IPublisher<,>), typeof(MessageBroker<,>));
            services.AddSingleton(typeof(ISubscriber<,>), typeof(MessageBroker<,>));

            // keyless PubSub
            services.AddSingleton(typeof(IMessageBroker<>), (options.DefaultHandlerRepository == DefaultHandlerRepository.ConcurrentDictionary)
                ? typeof(ConcurrentDictionaryMessageBroker<>)
                : typeof(ImmutableArrayMessageBroker<>));
            services.AddSingleton(typeof(IPublisher<>), typeof(MessageBroker<>));
            services.AddSingleton(typeof(ISubscriber<>), typeof(MessageBroker<>));

            // keyless PubSub async
            services.AddSingleton(typeof(IAsyncMessageBroker<>), typeof(ImmutableArrayAsyncMessageBroker<>));
            services.AddSingleton(typeof(IAsyncPublisher<>), typeof(AsyncMessageBroker<>));
            services.AddSingleton(typeof(IAsyncSubscriber<>), typeof(AsyncMessageBroker<>));

            // todo:automatically register IRequestHandler<T,T> => Handler

            // RequestAll
            services.AddSingleton(typeof(IRequestAllHandler<,>), typeof(RequestAllHandler<,>));

            // filters
            options.AddGlobalFilter(services);

            // TODO:search handler's filter?

            services.AddSingleton(typeof(MessagePipeDiagnosticsInfo));

            return services;
        }
    }
}

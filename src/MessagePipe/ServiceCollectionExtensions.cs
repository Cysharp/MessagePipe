using MessagePipe;
using MessagePipe.Internal;
using System;
using System.Linq;

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

            // keyless PubSub
            services.AddSingleton(typeof(MessageBrokerCore<>));
            services.AddSingleton(typeof(IPublisher<>), typeof(MessageBroker<>));
            services.AddSingleton(typeof(ISubscriber<>), typeof(MessageBroker<>));

            // keyless PubSub async
            services.AddSingleton(typeof(AsyncMessageBrokerCore<>));
            services.AddSingleton(typeof(IAsyncPublisher<>), typeof(AsyncMessageBroker<>));
            services.AddSingleton(typeof(IAsyncSubscriber<>), typeof(AsyncMessageBroker<>));

            // keyed PubSub
            services.AddSingleton(typeof(MessageBrokerCore<,>));
            services.AddSingleton(typeof(IPublisher<,>), typeof(MessageBroker<,>));
            services.AddSingleton(typeof(ISubscriber<,>), typeof(MessageBroker<,>));


            // RequestHandler
            services.AddSingleton(typeof(IRequestHandler<,>), typeof(RequestHandler<,>));
            // todo:automatically register IRequestHandler<T,T> => Handler


            // RequestAll
            services.AddSingleton(typeof(IRequestAllHandler<,>), typeof(RequestAllHandler<,>));
            services.AddSingleton(typeof(IAsyncRequestAllHandler<,>), typeof(AsyncRequestAllHandler<,>));

            // filters
            options.AddGlobalFilter(services);
            services.AddSingleton(typeof(FilterCache<,>));

            // TODO:search handler's filter?



            // others.
            services.AddSingleton(typeof(MessagePipeDiagnosticsInfo));

            return services;
        }

        public static IServiceCollection AddRequestHandler<T>(this IServiceCollection services)
            where T : IRequestHandler
        {
            var type = typeof(T).GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestHandlerCore<,>));
            if (type == null)
            {
                throw new ArgumentException($"{typeof(T).FullName} does not implement IRequestHandler<TRequest, TResponse>.");
            }

            services.AddSingleton(type, typeof(T));
            return services;
        }
    }
}

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

            var lifetime = (options.InstanceScope == InstanceScope.Scoped)
                ? ServiceLifetime.Scoped
                : ServiceLifetime.Singleton;

            // keyless PubSub
            Add(services, typeof(MessageBrokerCore<>), lifetime);
            Add(services, typeof(IPublisher<>), typeof(MessageBroker<>), lifetime);
            Add(services, typeof(ISubscriber<>), typeof(MessageBroker<>), lifetime);

            // keyless PubSub async
            Add(services, typeof(AsyncMessageBrokerCore<>), lifetime);
            Add(services, typeof(IAsyncPublisher<>), typeof(AsyncMessageBroker<>), lifetime);
            Add(services, typeof(IAsyncSubscriber<>), typeof(AsyncMessageBroker<>), lifetime);

            // keyed PubSub
            Add(services, typeof(MessageBrokerCore<,>), lifetime);
            Add(services, typeof(IPublisher<,>), typeof(MessageBroker<,>), lifetime);
            Add(services, typeof(ISubscriber<,>), typeof(MessageBroker<,>), lifetime);

            // keyed PubSub async
            Add(services, typeof(AsyncMessageBrokerCore<,>), lifetime);
            Add(services, typeof(IAsyncPublisher<,>), typeof(AsyncMessageBroker<,>), lifetime);
            Add(services, typeof(IAsyncSubscriber<,>), typeof(AsyncMessageBroker<,>), lifetime);

            // RequestHandler
            Add(services, typeof(IRequestHandler<,>), typeof(RequestHandler<,>), lifetime);
            Add(services, typeof(IAsyncRequestHandler<,>), typeof(AsyncRequestHandler<,>), lifetime);

            // RequestAll
            Add(services, typeof(IRequestAllHandler<,>), typeof(RequestAllHandler<,>), lifetime);
            Add(services, typeof(IAsyncRequestAllHandler<,>), typeof(AsyncRequestAllHandler<,>), lifetime);

            // filters
            options.AddGlobalFilter(services);
            services.AddSingleton(typeof(FilterCache<,>));

            // others.
            services.AddSingleton(typeof(MessagePipeDiagnosticsInfo));

            // TODO:search handler's filter?
            // todo:automatically register IRequestHandler<T,T> => Handler



            return services;
        }

        static void Add(IServiceCollection services, Type serviceType, ServiceLifetime lifetime)
        {
            services.Add(new ServiceDescriptor(serviceType, serviceType, lifetime));
        }

        static void Add(IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
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

        public static IServiceCollection AddAsyncRequestHandler<T>(this IServiceCollection services)
            where T : IAsyncRequestHandler
        {
            var type = typeof(T).GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAsyncRequestHandlerCore<,>));
            if (type == null)
            {
                throw new ArgumentException($"{typeof(T).FullName} does not implement IAsyncRequestHandler<TRequest, TResponse>.");
            }

            services.AddSingleton(type, typeof(T));
            return services;
        }
    }
}

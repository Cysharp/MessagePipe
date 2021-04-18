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
            services.AddSingleton(options); // add as singleton instance

            var scope = options.InstanceScope;

            // keyless PubSub
            Add(services, typeof(MessageBrokerCore<>), scope);
            Add(services, typeof(IPublisher<>), typeof(MessageBroker<>), scope);
            Add(services, typeof(ISubscriber<>), typeof(MessageBroker<>), scope);

            // keyless PubSub async
            Add(services, typeof(AsyncMessageBrokerCore<>), scope);
            Add(services, typeof(IAsyncPublisher<>), typeof(AsyncMessageBroker<>), scope);
            Add(services, typeof(IAsyncSubscriber<>), typeof(AsyncMessageBroker<>), scope);

            // keyed PubSub
            Add(services, typeof(MessageBrokerCore<,>), scope);
            Add(services, typeof(IPublisher<,>), typeof(MessageBroker<,>), scope);
            Add(services, typeof(ISubscriber<,>), typeof(MessageBroker<,>), scope);

            // keyed PubSub async
            Add(services, typeof(AsyncMessageBrokerCore<,>), scope);
            Add(services, typeof(IAsyncPublisher<,>), typeof(AsyncMessageBroker<,>), scope);
            Add(services, typeof(IAsyncSubscriber<,>), typeof(AsyncMessageBroker<,>), scope);

            // RequestHandler
            Add(services, typeof(IRequestHandler<,>), typeof(RequestHandler<,>), scope);
            Add(services, typeof(IAsyncRequestHandler<,>), typeof(AsyncRequestHandler<,>), scope);

            // RequestAll
            Add(services, typeof(IRequestAllHandler<,>), typeof(RequestAllHandler<,>), scope);
            Add(services, typeof(IAsyncRequestAllHandler<,>), typeof(AsyncRequestAllHandler<,>), scope);

            // filters
            options.AddGlobalFilter(services);
            services.AddSingleton(typeof(FilterCache<,>));

            // others.
            services.AddSingleton(typeof(MessagePipeDiagnosticsInfo));

            // TODO:search handler's filter?
            // todo:automatically register IRequestHandler<T,T> => Handler

            return services;
        }

        static void Add(IServiceCollection services, Type serviceType, InstanceScope scope)
        {
            var lifetime = (scope == InstanceScope.Scoped) ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
            services.Add(new ServiceDescriptor(serviceType, serviceType, lifetime));
        }

        static void Add(IServiceCollection services, Type serviceType, Type implementationType, InstanceScope scope)
        {
            var lifetime = (scope == InstanceScope.Scoped) ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
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

            var option = services.FirstOrDefault(x => x.ServiceType == typeof(MessagePipeOptions));
            if (option == null)
            {
                throw new ArgumentException($"Not yet added MessagePipeOptions, please call servcies.AddMessagePipe() before.");
            }

            Add(services, type, typeof(T), ((MessagePipeOptions)option.ImplementationInstance!).InstanceScope);
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

            var option = services.FirstOrDefault(x => x.ServiceType == typeof(MessagePipeOptions));
            if (option == null)
            {
                throw new ArgumentException($"Not yet added MessagePipeOptions, please call servcies.AddMessagePipe() before.");
            }

            Add(services, type, typeof(T), ((MessagePipeOptions)option.ImplementationInstance!).InstanceScope);
            return services;
        }
    }
}

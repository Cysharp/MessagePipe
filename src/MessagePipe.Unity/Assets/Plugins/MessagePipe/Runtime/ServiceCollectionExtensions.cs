#if !UNITY_2018_3_OR_NEWER

using MessagePipe;
using MessagePipe.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

            var lifetime = options.InstanceLifetime;

            // keyless PubSub
            services.Add(typeof(MessageBrokerCore<>), lifetime);
            services.Add(typeof(IPublisher<>), typeof(MessageBroker<>), lifetime);
            services.Add(typeof(ISubscriber<>), typeof(MessageBroker<>), lifetime);

            // keyless PubSub async
            services.Add(typeof(AsyncMessageBrokerCore<>), lifetime);
            services.Add(typeof(IAsyncPublisher<>), typeof(AsyncMessageBroker<>), lifetime);
            services.Add(typeof(IAsyncSubscriber<>), typeof(AsyncMessageBroker<>), lifetime);

            // keyed PubSub
            services.Add(typeof(MessageBrokerCore<,>), lifetime);
            services.Add(typeof(IPublisher<,>), typeof(MessageBroker<,>), lifetime);
            services.Add(typeof(ISubscriber<,>), typeof(MessageBroker<,>), lifetime);

            // keyed PubSub async
            services.Add(typeof(AsyncMessageBrokerCore<,>), lifetime);
            services.Add(typeof(IAsyncPublisher<,>), typeof(AsyncMessageBroker<,>), lifetime);
            services.Add(typeof(IAsyncSubscriber<,>), typeof(AsyncMessageBroker<,>), lifetime);

            // RequestHandler
            services.Add(typeof(IRequestHandler<,>), typeof(RequestHandler<,>), lifetime);
            services.Add(typeof(IAsyncRequestHandler<,>), typeof(AsyncRequestHandler<,>), lifetime);

            // RequestAll
            services.Add(typeof(IRequestAllHandler<,>), typeof(RequestAllHandler<,>), lifetime);
            services.Add(typeof(IAsyncRequestAllHandler<,>), typeof(AsyncRequestAllHandler<,>), lifetime);

            // filters
            options.AddGlobalFilter(services);
            services.AddSingleton(typeof(FilterCache<,>));
            services.AddSingleton(typeof(FilterAttachedMessageHandlerFactory));
            services.AddSingleton(typeof(FilterAttachedAsyncMessageHandlerFactory));

            // others.
            services.AddSingleton(typeof(MessagePipeDiagnosticsInfo));

            if (options.EnableAutowire)
            {
                // auto register filter and requesthandler

                if (options.autowireAssemblies == null && options.autowireTypes == null)
                {
                    var types = AutowireEngine.CollectFromCurrentDomain();
                    AutowireEngine.RegisterFromTypes(services, options, AutowireEngine.CollectFromCurrentDomain());
                }
                else
                {
                    var fromAssemblies = (options.autowireAssemblies != null)
                        ? AutowireEngine.CollectFromAssemblies(options.autowireAssemblies)
                        : Enumerable.Empty<Type>();

                    var types = (options.autowireTypes != null)
                        ? options.autowireTypes
                        : Enumerable.Empty<Type>();

                    AutowireEngine.RegisterFromTypes(services, options, fromAssemblies.Concat(types).Distinct());
                }
            }

            return services;
        }

        public static IServiceCollection AddMessageHandlerFilter<T>(this IServiceCollection services)
            where T : MessageHandlerFilter
        {
            services.TryAddTransient<T>();
            return services;
        }

        public static IServiceCollection AddAsyncMessageHandlerFilter<T>(this IServiceCollection services)
            where T : AsyncMessageHandlerFilter
        {
            services.TryAddTransient<T>();
            return services;
        }

        public static IServiceCollection AddRequestHandlerFilter<T>(this IServiceCollection services)
            where T : RequestHandlerFilter
        {
            services.TryAddTransient<T>();
            return services;
        }

        public static IServiceCollection AddAsyncRequestHandlerFilter<T>(this IServiceCollection services)
            where T : AsyncRequestHandlerFilter
        {
            services.TryAddTransient<T>();
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

            var option = services.FirstOrDefault(x => x.ServiceType == typeof(MessagePipeOptions));
            if (option == null)
            {
                throw new ArgumentException($"Not yet added MessagePipeOptions, please call servcies.AddMessagePipe() before.");
            }

            services.Add(type, typeof(T), ((MessagePipeOptions)option.ImplementationInstance!).InstanceLifetime);
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

            services.Add(type, typeof(T), ((MessagePipeOptions)option.ImplementationInstance!).InstanceLifetime);
            return services;
        }

        internal static void Add(this IServiceCollection services, Type serviceType, InstanceLifetime lifetime)
        {
            var lt = (lifetime == InstanceLifetime.Scoped) ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
            services.Add(new ServiceDescriptor(serviceType, serviceType, lt));
        }

        internal static void Add(this IServiceCollection services, Type serviceType, Type implementationType, InstanceLifetime lifetime)
        {
            var lt = (lifetime == InstanceLifetime.Scoped) ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
            services.Add(new ServiceDescriptor(serviceType, implementationType, lt));
        }
    }
}

#endif
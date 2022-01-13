using MessagePipe;
using MessagePipe.Internal;
#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
#endif
using System;
using System.Collections.Generic;
using System.Linq;

namespace MessagePipe
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
            services.AddSingleton(typeof(MessagePipeDiagnosticsInfo));
            services.AddSingleton(typeof(EventFactory));

            // filters.
            // attribute and order is deterministic at compile, so use Singleton lifetime of cache.
            services.AddSingleton(typeof(AttributeFilterProvider<MessageHandlerFilterAttribute>));
            services.AddSingleton(typeof(AttributeFilterProvider<AsyncMessageHandlerFilterAttribute>));
            services.AddSingleton(typeof(AttributeFilterProvider<RequestHandlerFilterAttribute>));
            services.AddSingleton(typeof(AttributeFilterProvider<AsyncRequestHandlerFilterAttribute>));
            services.AddSingleton(typeof(FilterAttachedMessageHandlerFactory));
            services.AddSingleton(typeof(FilterAttachedAsyncMessageHandlerFactory));
            services.AddSingleton(typeof(FilterAttachedRequestHandlerFactory));
            services.AddSingleton(typeof(FilterAttachedAsyncRequestHandlerFactory));
            foreach (var item in options.GetGlobalFilterTypes())
            {
                services.TryAddTransient(item); // filter itself is Transient
            }

#if !UNITY_2018_3_OR_NEWER
            // open generics implemntations(.NET Only)

            {
                var lifetime = options.InstanceLifetime; // pubsub lifetime

                // keyless PubSub
                services.Add(typeof(MessageBrokerCore<>), lifetime);
                services.Add(typeof(IPublisher<>), typeof(MessageBroker<>), lifetime);
                services.Add(typeof(ISubscriber<>), typeof(MessageBroker<>), lifetime);
                services.Add(typeof(BufferedMessageBrokerCore<>), lifetime);
                services.Add(typeof(IBufferedPublisher<>), typeof(BufferedMessageBroker<>), lifetime);
                services.Add(typeof(IBufferedSubscriber<>), typeof(BufferedMessageBroker<>), lifetime);

                // keyless-variation
                services.Add(typeof(SingletonMessageBrokerCore<>), InstanceLifetime.Singleton);
                services.Add(typeof(ISingletonPublisher<>), typeof(SingletonMessageBroker<>), InstanceLifetime.Singleton);
                services.Add(typeof(ISingletonSubscriber<>), typeof(SingletonMessageBroker<>), InstanceLifetime.Singleton);
                services.Add(typeof(ScopedMessageBrokerCore<>), InstanceLifetime.Scoped);
                services.Add(typeof(IScopedPublisher<>), typeof(ScopedMessageBroker<>), InstanceLifetime.Scoped);
                services.Add(typeof(IScopedSubscriber<>), typeof(ScopedMessageBroker<>), InstanceLifetime.Scoped);

                // keyless PubSub async
                services.Add(typeof(AsyncMessageBrokerCore<>), lifetime);
                services.Add(typeof(IAsyncPublisher<>), typeof(AsyncMessageBroker<>), lifetime);
                services.Add(typeof(IAsyncSubscriber<>), typeof(AsyncMessageBroker<>), lifetime);
                services.Add(typeof(BufferedAsyncMessageBrokerCore<>), lifetime);
                services.Add(typeof(IBufferedAsyncPublisher<>), typeof(BufferedAsyncMessageBroker<>), lifetime);
                services.Add(typeof(IBufferedAsyncSubscriber<>), typeof(BufferedAsyncMessageBroker<>), lifetime);

                // keyless-async-variation
                services.Add(typeof(SingletonAsyncMessageBrokerCore<>), InstanceLifetime.Singleton);
                services.Add(typeof(ISingletonAsyncPublisher<>), typeof(SingletonAsyncMessageBroker<>), InstanceLifetime.Singleton);
                services.Add(typeof(ISingletonAsyncSubscriber<>), typeof(SingletonAsyncMessageBroker<>), InstanceLifetime.Singleton);
                services.Add(typeof(ScopedAsyncMessageBrokerCore<>), InstanceLifetime.Scoped);
                services.Add(typeof(IScopedAsyncPublisher<>), typeof(ScopedAsyncMessageBroker<>), InstanceLifetime.Scoped);
                services.Add(typeof(IScopedAsyncSubscriber<>), typeof(ScopedAsyncMessageBroker<>), InstanceLifetime.Scoped);

                // keyed PubSub
                services.Add(typeof(MessageBrokerCore<,>), lifetime);
                services.Add(typeof(IPublisher<,>), typeof(MessageBroker<,>), lifetime);
                services.Add(typeof(ISubscriber<,>), typeof(MessageBroker<,>), lifetime);

                // keyed-variation
                services.Add(typeof(SingletonMessageBrokerCore<,>), InstanceLifetime.Singleton);
                services.Add(typeof(ISingletonPublisher<,>), typeof(SingletonMessageBroker<,>), InstanceLifetime.Singleton);
                services.Add(typeof(ISingletonSubscriber<,>), typeof(SingletonMessageBroker<,>), InstanceLifetime.Singleton);
                services.Add(typeof(ScopedMessageBrokerCore<,>), InstanceLifetime.Scoped);
                services.Add(typeof(IScopedPublisher<,>), typeof(ScopedMessageBroker<,>), InstanceLifetime.Scoped);
                services.Add(typeof(IScopedSubscriber<,>), typeof(ScopedMessageBroker<,>), InstanceLifetime.Scoped);

                // keyed PubSub async
                services.Add(typeof(AsyncMessageBrokerCore<,>), lifetime);
                services.Add(typeof(IAsyncPublisher<,>), typeof(AsyncMessageBroker<,>), lifetime);
                services.Add(typeof(IAsyncSubscriber<,>), typeof(AsyncMessageBroker<,>), lifetime);

                // keyed-async-variation
                services.Add(typeof(SingletonAsyncMessageBrokerCore<,>), InstanceLifetime.Singleton);
                services.Add(typeof(ISingletonAsyncPublisher<,>), typeof(SingletonAsyncMessageBroker<,>), InstanceLifetime.Singleton);
                services.Add(typeof(ISingletonAsyncSubscriber<,>), typeof(SingletonAsyncMessageBroker<,>), InstanceLifetime.Singleton);
                services.Add(typeof(ScopedAsyncMessageBrokerCore<,>), InstanceLifetime.Scoped);
                services.Add(typeof(IScopedAsyncPublisher<,>), typeof(ScopedAsyncMessageBroker<,>), InstanceLifetime.Scoped);
                services.Add(typeof(IScopedAsyncSubscriber<,>), typeof(ScopedAsyncMessageBroker<,>), InstanceLifetime.Scoped);
            }

            var lifetime2 = options.RequestHandlerLifetime; // requesthandler lifetime

            // RequestHandler
            services.Add(typeof(IRequestHandler<,>), typeof(RequestHandler<,>), lifetime2);
            services.Add(typeof(IAsyncRequestHandler<,>), typeof(AsyncRequestHandler<,>), lifetime2);

            // RequestAll
            services.Add(typeof(IRequestAllHandler<,>), typeof(RequestAllHandler<,>), lifetime2);
            services.Add(typeof(IAsyncRequestAllHandler<,>), typeof(AsyncRequestAllHandler<,>), lifetime2);

            // auto registration is .NET only.
            if (options.EnableAutoRegistration)
            {
                // auto register filter and requesthandler
                // request handler is option's lifetime, filter is transient
                if (options.autoregistrationAssemblies == null && options.autoregistrationTypes == null)
                {
                    AddRequestHandlerAndFilterFromTypes(services, lifetime2, TypeCollector.CollectFromCurrentDomain());
                }
                else
                {
                    var fromAssemblies = (options.autoregistrationAssemblies != null)
                        ? TypeCollector.CollectFromAssemblies(options.autoregistrationAssemblies)
                        : Enumerable.Empty<Type>();
                    var types = options.autoregistrationTypes ?? Enumerable.Empty<Type>();

                    AddRequestHandlerAndFilterFromTypes(services, lifetime2, fromAssemblies.Concat(types).Distinct());
                }
            }

#endif

            return services;
        }

#if !UNITY_2018_3_OR_NEWER

        public static IServiceCollection AddMessageHandlerFilter<T>(this IServiceCollection services)
            where T : class, IMessageHandlerFilter
        {
            services.TryAddTransient<T>();
            return services;
        }

        public static IServiceCollection AddAsyncMessageHandlerFilter<T>(this IServiceCollection services)
            where T : class, IAsyncMessageHandlerFilter
        {
            services.TryAddTransient<T>();
            return services;
        }

        public static IServiceCollection AddRequestHandlerFilter<T>(this IServiceCollection services)
            where T : class, IRequestHandlerFilter
        {
            services.TryAddTransient<T>();
            return services;
        }

        public static IServiceCollection AddAsyncRequestHandlerFilter<T>(this IServiceCollection services)
            where T : class, IAsyncRequestHandlerFilter
        {
            services.TryAddTransient<T>();
            return services;
        }

        public static IServiceCollection AddRequestHandler(this IServiceCollection services, Type type)
        {
            return AddRequestHandlerCore(services, type, typeof(IRequestHandlerCore<,>));
        }

        public static IServiceCollection AddRequestHandler<T>(this IServiceCollection services)
            where T : IRequestHandler
        {
            return AddRequestHandler(services, typeof(T));
        }

        public static IServiceCollection AddAsyncRequestHandler(this IServiceCollection services, Type type)
        {
            return AddRequestHandlerCore(services, type, typeof(IAsyncRequestHandlerCore<,>));
        }

        public static IServiceCollection AddAsyncRequestHandler<T>(this IServiceCollection services)
            where T : IAsyncRequestHandler
        {
            return AddAsyncRequestHandler(services, typeof(T));
        }

        static IServiceCollection AddRequestHandlerCore(IServiceCollection services, Type type, Type coreType)
        {
            var options = services.FirstOrDefault(x => x.ServiceType == typeof(MessagePipeOptions));
            if (options == null)
            {
                throw new ArgumentException($"Not yet added MessagePipeOptions, please call servcies.AddMessagePipe() before.");
            }
            var isAsync = (coreType == typeof(IAsyncRequestHandlerCore<,>));

            var registered = false;
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == coreType)
                {
                    var iType = interfaceType;
                    if (type.IsGenericType && !type.IsConstructedGenericType)
                    {
                        if (!interfaceType.GetGenericArguments().All(x => x.IsGenericParameter))
                        {
                            throw new ArgumentException("partial open generic type is not supported. Type:" + type.FullName);
                        }

                        iType = interfaceType.GetGenericTypeDefinition();
                    }

                    registered = true;
                    services.Add(iType, type, ((MessagePipeOptions)options.ImplementationInstance!).RequestHandlerLifetime);
                }
            }

            if (!registered)
            {
                throw new ArgumentException($"{type.FullName} does not implement {coreType.Name.Replace("Core", "")}.");
            }
            else if (isAsync)
            {
                AsyncRequestHandlerRegistory.Add(coreType);
            }

            return services;
        }

        public static IServiceCollection AddInMemoryDistributedMessageBroker(this IServiceCollection services)
        {
            var options = services.FirstOrDefault(x => x.ServiceType == typeof(MessagePipeOptions));
            if (options == null)
            {
                throw new ArgumentException($"Not yet added MessagePipeOptions, please call servcies.AddMessagePipe() before.");
            }

            var lifetime = ((MessagePipeOptions)options.ImplementationInstance!).InstanceLifetime;
            services.Add(typeof(IDistributedPublisher<,>), typeof(InMemoryDistributedPublisher<,>), lifetime);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(InMemoryDistributedSubscriber<,>), lifetime);

            return services;
        }

        internal static void Add(this IServiceCollection services, Type serviceType, InstanceLifetime lifetime)
        {
            var lt = (lifetime == InstanceLifetime.Scoped) ? ServiceLifetime.Scoped
                   : (lifetime == InstanceLifetime.Singleton) ? ServiceLifetime.Singleton
                   : ServiceLifetime.Transient;
            services.Add(new ServiceDescriptor(serviceType, serviceType, lt));
        }

        internal static void Add(this IServiceCollection services, Type serviceType, Type implementationType, InstanceLifetime lifetime)
        {
            var lt = (lifetime == InstanceLifetime.Scoped) ? ServiceLifetime.Scoped
                   : (lifetime == InstanceLifetime.Singleton) ? ServiceLifetime.Singleton
                   : ServiceLifetime.Transient;
            services.Add(new ServiceDescriptor(serviceType, implementationType, lt));
        }

        static void AddRequestHandlerAndFilterFromTypes(IServiceCollection services, InstanceLifetime requestHandlerLifetime, IEnumerable<Type> targetTypes)
        {
            foreach (var objectType in targetTypes)
            {
                if (objectType.IsInterface || objectType.IsAbstract) continue;
                if (objectType.GetCustomAttributes(typeof(IgnoreAutoRegistration), false).Length != 0) continue;

                foreach (var interfaceType in objectType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRequestHandlerCore<,>))
                    {
                        if (!objectType.IsGenericType || objectType.IsConstructedGenericType)
                        {
                            services.Add(interfaceType, objectType, requestHandlerLifetime);
                        }
                        else if (interfaceType.GetGenericArguments().All(x => x.IsGenericParameter))
                        {
                            services.Add(typeof(IRequestHandlerCore<,>), objectType, requestHandlerLifetime);
                        }
                        continue;
                    }

                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IAsyncRequestHandlerCore<,>))
                    {
                        if (!objectType.IsGenericType || objectType.IsConstructedGenericType)
                        {
                            services.Add(interfaceType, objectType, requestHandlerLifetime);
                        }
                        else if (interfaceType.GetGenericArguments().All(x => x.IsGenericParameter))
                        {
                            services.Add(typeof(IAsyncRequestHandlerCore<,>), objectType, requestHandlerLifetime);
                        }

                        AsyncRequestHandlerRegistory.Add(objectType);
                        continue;
                    }
                }

                foreach (var baseType in objectType.GetBaseTypes())
                {
                    if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(MessageHandlerFilter<>))
                    {
                        services.TryAddTransient(objectType);
                        goto NEXT_TYPE;
                    }

                    if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(AsyncMessageHandlerFilter<>))
                    {
                        services.TryAddTransient(objectType);
                        goto NEXT_TYPE;
                    }

                    if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(RequestHandlerFilter<,>))
                    {
                        services.TryAddTransient(objectType);
                        goto NEXT_TYPE;
                    }

                    if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(AsyncRequestHandlerFilter<,>))
                    {
                        services.TryAddTransient(objectType);
                        goto NEXT_TYPE;
                    }
                }

            NEXT_TYPE:
                continue;
            }
        }
#endif
    }
}
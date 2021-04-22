#if !UNITY_2018_3_OR_NEWER

using MessagePipe;
using MessagePipe.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
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

            var lifetime = options.InstanceLifetime; // lifetime is Singleton or Scoped

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

            // others.
            services.AddSingleton(typeof(MessagePipeDiagnosticsInfo));

            // auto registration is .NET only.
            if (options.EnableAutoRegistration)
            {
                // auto register filter and requesthandler
                // request handler is option's lifetime, filter is transient
                if (options.autoregistrationAssemblies == null && options.autoregistrationTypes == null)
                {
                    AddRequestHandlerAndFilterFromTypes(services, lifetime, TypeCollector.CollectFromCurrentDomain());
                }
                else
                {
                    var fromAssemblies = (options.autoregistrationAssemblies != null)
                        ? TypeCollector.CollectFromAssemblies(options.autoregistrationAssemblies)
                        : Enumerable.Empty<Type>();
                    var types = options.autoregistrationTypes ?? Enumerable.Empty<Type>();

                    AddRequestHandlerAndFilterFromTypes(services, lifetime, fromAssemblies.Concat(types).Distinct());
                }
            }

            return services;
        }

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
            var interfaceType = type.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == coreType);
            if (interfaceType == null)
            {
                throw new ArgumentException($"{type.FullName} does not implement {coreType.Name.Replace("Core", "")}.");
            }

            var options = services.FirstOrDefault(x => x.ServiceType == typeof(MessagePipeOptions));
            if (options == null)
            {
                throw new ArgumentException($"Not yet added MessagePipeOptions, please call servcies.AddMessagePipe() before.");
            }

            if (type.IsGenericType && !type.IsConstructedGenericType)
            {
                if (!interfaceType.GetGenericArguments().All(x => x.IsGenericParameter))
                {
                    throw new ArgumentException("partial open generic type is not supported. Type:" + type.FullName);
                }

                interfaceType = interfaceType.GetGenericTypeDefinition();
            }

            services.Add(interfaceType, type, ((MessagePipeOptions)options.ImplementationInstance!).InstanceLifetime);
            return services;
        }

        public static void AddInMemoryDistributedMessageBroker(this IServiceCollection services)
        {
            var options = services.FirstOrDefault(x => x.ServiceType == typeof(MessagePipeOptions));
            if (options == null)
            {
                throw new ArgumentException($"Not yet added MessagePipeOptions, please call servcies.AddMessagePipe() before.");
            }

            var lifetime = ((MessagePipeOptions)options.ImplementationInstance!).InstanceLifetime;
            services.Add(typeof(IDistributedPublisher<,>), typeof(InMemoryDistributedPublisher<,>), lifetime);
            services.Add(typeof(IDistributedSubscriber<,>), typeof(InMemoryDistributedSubscriber<,>), lifetime);
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
                        goto NEXT_TYPE;
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
                        goto NEXT_TYPE;
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

        static IEnumerable<Type> GetBaseTypes(this Type? t)
        {
            if (t == null) yield break;
            t = t.BaseType;
            while (t != null)
            {
                yield return t;
                t = t.BaseType;
            }
        }
    }
}

#endif
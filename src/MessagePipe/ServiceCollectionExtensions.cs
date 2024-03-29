using MessagePipe;
using MessagePipe.Internal;
#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if !UNITY_2018_3_OR_NEWER
namespace Microsoft.Extensions.DependencyInjection
#else
namespace MessagePipe
#endif
{
    /// <summary>
    /// An interface for configuring MessagePipe services.
    /// </summary>
    public interface IMessagePipeBuilder
    {
        IServiceCollection Services { get; }
    }

    public class MessagePipeBuilder : IMessagePipeBuilder
    {
        public IServiceCollection Services { get; }

        public MessagePipeBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IMessagePipeBuilder AddMessagePipe(this IServiceCollection services)
        {
            return AddMessagePipe(services, _ => { });
        }

        public static IMessagePipeBuilder AddMessagePipe(this IServiceCollection services, Action<MessagePipeOptions> configure)
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

#if !UNITY_2018_3_OR_NEWER || (MESSAGEPIPE_OPENGENERICS_SUPPORT && UNITY_2022_1_OR_NEWER)
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

#endif
#if !UNITY_2018_3_OR_NEWER
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

            return new MessagePipeBuilder(services);
        }

#if !UNITY_2018_3_OR_NEWER

        public static IMessagePipeBuilder AddMessageHandlerFilter<T>(this IMessagePipeBuilder builder)
            where T : class, IMessageHandlerFilter
        {
            builder.Services.TryAddTransient<T>();
            return builder;
        }

        public static IMessagePipeBuilder AddAsyncMessageHandlerFilter<T>(this IMessagePipeBuilder builder)
            where T : class, IAsyncMessageHandlerFilter
        {
            builder.Services.TryAddTransient<T>();
            return builder;
        }

        public static IMessagePipeBuilder AddRequestHandlerFilter<T>(this IMessagePipeBuilder builder)
            where T : class, IRequestHandlerFilter
        {
            builder.Services.TryAddTransient<T>();
            return builder;
        }

        public static IMessagePipeBuilder AddAsyncRequestHandlerFilter<T>(this IMessagePipeBuilder builder)
            where T : class, IAsyncRequestHandlerFilter
        {
            builder.Services.TryAddTransient<T>();
            return builder;
        }

        public static IMessagePipeBuilder AddRequestHandler(this IMessagePipeBuilder builder, Type type)
        {
            return AddRequestHandlerCore(builder, type, typeof(IRequestHandlerCore<,>));
        }

        public static IMessagePipeBuilder AddRequestHandler<T>(this IMessagePipeBuilder builder)
            where T : IRequestHandler
        {
            return AddRequestHandler(builder, typeof(T));
        }

        public static IMessagePipeBuilder AddAsyncRequestHandler(this IMessagePipeBuilder builder, Type type)
        {
            return AddRequestHandlerCore(builder, type, typeof(IAsyncRequestHandlerCore<,>));
        }

        public static IMessagePipeBuilder AddAsyncRequestHandler<T>(this IMessagePipeBuilder builder)
            where T : IAsyncRequestHandler
        {
            return AddAsyncRequestHandler(builder, typeof(T));
        }

        static IMessagePipeBuilder AddRequestHandlerCore(IMessagePipeBuilder builder, Type type, Type coreType)
        {
            var options = builder.Services.FirstOrDefault(x => x.ServiceType == typeof(MessagePipeOptions));
            if (options == null)
            {
                throw new ArgumentException($"Not yet added MessagePipeOptions, please call services.AddMessagePipe() before.");
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
                    builder.Services.Add(iType, type, ((MessagePipeOptions)options.ImplementationInstance!).RequestHandlerLifetime);
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

            return builder;
        }

        public static IMessagePipeBuilder AddInMemoryDistributedMessageBroker(this IMessagePipeBuilder builder)
        {
            var options = builder.Services.FirstOrDefault(x => x.ServiceType == typeof(MessagePipeOptions));
            if (options == null)
            {
                throw new ArgumentException($"Not yet added MessagePipeOptions, please call services.AddMessagePipe() before.");
            }

            var lifetime = ((MessagePipeOptions)options.ImplementationInstance!).InstanceLifetime;
            builder.Services.Add(typeof(IDistributedPublisher<,>), typeof(InMemoryDistributedPublisher<,>), lifetime);
            builder.Services.Add(typeof(IDistributedSubscriber<,>), typeof(InMemoryDistributedSubscriber<,>), lifetime);

            return builder;
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
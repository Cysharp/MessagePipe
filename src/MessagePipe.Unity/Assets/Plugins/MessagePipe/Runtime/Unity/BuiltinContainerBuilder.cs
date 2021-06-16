using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MessagePipe
{
    public partial class BuiltinContainerBuilder
    {
        public BuiltinContainerBuilder()
        {
        }

        public BuiltinContainerBuilder AddMessagePipe()
        {
            return AddMessagePipe(_ => { });
        }

        public BuiltinContainerBuilder AddMessagePipe(Action<MessagePipeOptions> configure)
        {
            ServiceCollectionExtensions.AddMessagePipe(this, configure);
            return this;
        }

        public IServiceProvider BuildServiceProvider()
        {
            return new BuiltinContainerBuilderServiceProvider(this);
        }

        /// <summary>Register IPublisher[TMessage] and ISubscriber[TMessage](includes Async/Buffered) to container builder.</summary>
        public BuiltinContainerBuilder AddMessageBroker<TMessage>()
        {
            var services = this;

            // keyless PubSub
            services.AddSingleton(typeof(MessageBrokerCore<TMessage>));
            services.AddSingleton(typeof(IPublisher<TMessage>), typeof(MessageBroker<TMessage>));
            services.AddSingleton(typeof(ISubscriber<TMessage>), typeof(MessageBroker<TMessage>));

            // keyless PubSub async
            services.AddSingleton(typeof(AsyncMessageBrokerCore<TMessage>));
            services.AddSingleton(typeof(IAsyncPublisher<TMessage>), typeof(AsyncMessageBroker<TMessage>));
            services.AddSingleton(typeof(IAsyncSubscriber<TMessage>), typeof(AsyncMessageBroker<TMessage>));

            // keyless buffered PubSub
            services.AddSingleton(typeof(BufferedMessageBrokerCore<TMessage>));
            services.AddSingleton(typeof(IBufferedPublisher<TMessage>), typeof(BufferedMessageBroker<TMessage>));
            services.AddSingleton(typeof(IBufferedSubscriber<TMessage>), typeof(BufferedMessageBroker<TMessage>));

            // keyless buffered PubSub async
            services.AddSingleton(typeof(BufferedAsyncMessageBrokerCore<TMessage>));
            services.AddSingleton(typeof(IBufferedAsyncPublisher<TMessage>), typeof(BufferedAsyncMessageBroker<TMessage>));
            services.AddSingleton(typeof(IBufferedAsyncSubscriber<TMessage>), typeof(BufferedAsyncMessageBroker<TMessage>));

            return this;
        }

        /// <summary>Register IPublisher[TKey, TMessage] and ISubscriber[TKey, TMessage](includes Async) to container builder.</summary>
        public BuiltinContainerBuilder AddMessageBroker<TKey, TMessage>()
        {
            var services = this;

            // keyed PubSub
            services.AddSingleton(typeof(MessageBrokerCore<TKey, TMessage>));
            services.AddSingleton(typeof(IPublisher<TKey, TMessage>), typeof(MessageBroker<TKey, TMessage>));
            services.AddSingleton(typeof(ISubscriber<TKey, TMessage>), typeof(MessageBroker<TKey, TMessage>));

            // keyed PubSub async
            services.AddSingleton(typeof(AsyncMessageBrokerCore<TKey, TMessage>));
            services.AddSingleton(typeof(IAsyncPublisher<TKey, TMessage>), typeof(AsyncMessageBroker<TKey, TMessage>));
            services.AddSingleton(typeof(IAsyncSubscriber<TKey, TMessage>), typeof(AsyncMessageBroker<TKey, TMessage>));

            return this;
        }

        /// <summary>Register IRequestHandler[TRequest, TResponse] to container builder.</summary>
        public BuiltinContainerBuilder AddRequestHandler<TRequest, TResponse, THandler>()
            where THandler : IRequestHandler
        {
            var services = this;

            services.AddSingleton(typeof(IRequestHandlerCore<TRequest, TResponse>), typeof(THandler));
            services.AddSingleton(typeof(IRequestHandler<TRequest, TResponse>), typeof(RequestHandler<TRequest, TResponse>));

            return this;
        }

        /// <summary>Register IAsyncRequestHandler[TRequest, TResponse] to container builder.</summary>
        public BuiltinContainerBuilder AddAsyncRequestHandler<TRequest, TResponse, THandler>()
            where THandler : IAsyncRequestHandler
        {
            var services = this;

            services.AddSingleton(typeof(IAsyncRequestHandlerCore<TRequest, TResponse>), typeof(THandler));
            services.AddSingleton(typeof(IAsyncRequestHandler<TRequest, TResponse>), typeof(AsyncRequestHandler<TRequest, TResponse>));

            AsyncRequestHandlerRegistory.Add(typeof(TRequest), typeof(TResponse), typeof(THandler));

            return this;
        }

        public BuiltinContainerBuilder AddMessageHandlerFilter<T>()
            where T : class, IMessageHandlerFilter
        {
            this.TryAddTransient(typeof(T));
            return this;
        }

        public BuiltinContainerBuilder AddAsyncMessageHandlerFilter<T>()
            where T : class, IAsyncMessageHandlerFilter
        {
            this.TryAddTransient(typeof(T));
            return this;
        }

        public BuiltinContainerBuilder AddRequestHandlerFilter<T>()
            where T : class, IRequestHandlerFilter
        {
            this.TryAddTransient(typeof(T));
            return this;
        }

        public BuiltinContainerBuilder AddAsyncRequestHandlerFilter<T>()
            where T : class, IAsyncRequestHandlerFilter
        {
            this.TryAddTransient(typeof(T));
            return this;
        }
    }

    // DI Container builder.
    public partial class BuiltinContainerBuilder : IServiceCollection
    {
        internal readonly Dictionary<Type, object> singletonInstances = new Dictionary<Type, object>();
        internal readonly List<(Type serviceType, Type implementationType)> singleton = new List<(Type serviceType, Type implementationType)>();
        internal readonly List<(Type serviceType, Type implementationType)> transient = new List<(Type serviceType, Type implementationType)>();

        public void AddSingleton<T>(T instance)
        {
            singletonInstances[typeof(T)] = instance;
        }

        public void AddSingleton(Type type)
        {
            singleton.Add((type, type));
        }

        public void AddTransient(Type type)
        {
            transient.Add((type, type));
        }

        public void TryAddTransient(Type type)
        {
            foreach (var item in transient)
            {
                if (item.serviceType == type)
                {
                    return;
                }
            }

            transient.Add((type, type));
        }

        public void AddSingleton(Type serviceType, Type implementationType)
        {
            singleton.Add((serviceType, implementationType));
        }



        public void Add(Type serviceType, Type implementationType, InstanceLifetime lifetime)
        {
            if (lifetime == InstanceLifetime.Scoped || lifetime == InstanceLifetime.Singleton)
            {
                singleton.Add((serviceType, implementationType));
            }
            else // Transient
            {
                transient.Add((serviceType, implementationType));
            }
        }
    }

    class BuiltinContainerBuilderServiceProvider : IServiceProvider
    {
        readonly Dictionary<Type, Lazy<object>> singletonInstances;
        readonly Dictionary<Type, ServiceProviderType> transientTypes;

        public BuiltinContainerBuilderServiceProvider(BuiltinContainerBuilder builder)
        {
            this.singletonInstances = new Dictionary<Type, Lazy<object>>(builder.singletonInstances.Count + builder.singleton.Count);
            this.transientTypes = new Dictionary<Type, ServiceProviderType>(builder.transient.Count);

            foreach (var item in builder.singletonInstances)
            {
                this.singletonInstances[item.Key] = new Lazy<object>(() => item.Value);
            }

            foreach (var item in builder.singleton)
            {
                var implType = item.implementationType;
                this.singletonInstances[item.serviceType] = new Lazy<object>(() => new ServiceProviderType(implType).Instantiate(this, 0)); // memo: require to lazy with parameter(pass depth).
            }

            foreach (var item in builder.transient)
            {
                this.transientTypes[item.serviceType] = new ServiceProviderType(item.implementationType);
            }
        }

        public object GetService(Type serviceType)
        {
            return GetService(serviceType, 0);
        }

        public object GetService(Type serviceType, int depth)
        {
            if (serviceType == typeof(IServiceProvider))
            {
                return this; // resolve self
            }

            if (singletonInstances.TryGetValue(serviceType, out var value))
            {
                return value.Value; // return Lazy<T>.Value
            }

            if (transientTypes.TryGetValue(serviceType, out var providerType))
            {
                return providerType.Instantiate(this, depth);
            }

            return null;
        }
    }

    class ServiceProviderType
    {
        readonly Type type;
        readonly ConstructorInfo ctor;
        readonly ParameterInfo[] parameters;

        public ServiceProviderType(Type type)
        {
            var info = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Select(x => new { ctor = x, parameters = x.GetParameters() })
                .OrderByDescending(x => x.parameters.Length) // MaxBy
                .FirstOrDefault();

            if (!type.IsValueType && info == null)
            {
                throw new InvalidOperationException("ConsturoctorInfo is not found, is stripped? Type:" + type.FullName);
            }

            this.type = type;
            this.ctor = info?.ctor;
            this.parameters = info?.parameters;
        }

        public object Instantiate(BuiltinContainerBuilderServiceProvider provider, int depth)
        {
            if (ctor == null)
            {
                return Activator.CreateInstance(type);
            }

            if (parameters.Length == 0)
            {
                return ctor.Invoke(Array.Empty<object>());
            }
            if (depth > 15)
            {
                throw new InvalidOperationException("Parameter too recursively: " + type.FullName);
            }

            var p = new object[parameters.Length];
            for (int i = 0; i < p.Length; i++)
            {
                p[i] = provider.GetService(parameters[i].ParameterType, depth + 1);
            }

            return ctor.Invoke(p);
        }
    }
}
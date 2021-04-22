using System;
using VContainer;

namespace MessagePipe
{
    internal struct ContainerBuilderProxy : IServiceCollection
    {
        readonly IContainerBuilder builder;

        public ContainerBuilderProxy(IContainerBuilder builder)
        {
            this.builder = builder;
        }

        public void TryAddTransient(Type type)
        {
            if (!builder.Exists(type))
            {
                builder.Register(type, Lifetime.Transient);
            }
        }

        public void AddSingleton<T>(T instance)
        {
            builder.RegisterInstance<T>(instance);
        }

        public void AddSingleton(Type type)
        {
            builder.Register(type, Lifetime.Singleton);
        }

        public void Add(Type type, Lifetime lifetime)
        {
            builder.Register(type, lifetime);
        }

        public void Add(Type serviceType, Type implementationType, Lifetime lifetime)
        {
            builder.Register(implementationType, lifetime).As(serviceType);
        }
    }

    public sealed class ObjectResolverProxy : IServiceProvider
    {
        IObjectResolver resolver;

        public ObjectResolverProxy(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }

        public object GetService(Type serviceType)
        {
            return resolver.Resolve(serviceType);
        }
    }
}

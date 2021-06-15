using MessagePipe.VContainer;
using System;
using VContainer;

namespace MessagePipe.VContainer
{
    internal class ContainerBuilderProxy : IServiceCollection
    {
        readonly IContainerBuilder builder;

        public ContainerBuilderProxy(IContainerBuilder builder)
        {
            this.builder = builder;
        }

        public void AddTransient(Type type)
        {
            builder.Register(type, Lifetime.Transient);
        }

        public void TryAddTransient(Type type)
        {
            if (!builder.Exists(type, true))
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

        public void Add(Type serviceType, Type implementationType, InstanceLifetime lifetime)
        {
            var l = (lifetime == InstanceLifetime.Singleton) ? Lifetime.Singleton
                  : (lifetime == InstanceLifetime.Scoped) ? Lifetime.Scoped
                  : Lifetime.Transient;

            builder.Register(implementationType, l).As(serviceType);
        }
    }

    [Preserve]
    public sealed class ObjectResolverProxy : IServiceProvider
    {
        IObjectResolver resolver;

        [Preserve]
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

namespace MessagePipe
{
    public static class ObjectResolverExtensions
    {
        public static IServiceProvider AsServiceProvider(this IObjectResolver resolver)
        {
            return new ObjectResolverProxy(resolver);
        }
    }
}

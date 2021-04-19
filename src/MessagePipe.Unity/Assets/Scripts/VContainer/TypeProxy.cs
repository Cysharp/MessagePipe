using System;
using VContainer;

namespace MessagePipe
{
    public sealed class ContainerBuilderProxy : IServiceCollection
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
    }

    public sealed class ObjectResolverProxy : IServiceProvider
    {
        readonly IObjectResolver resolver;

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

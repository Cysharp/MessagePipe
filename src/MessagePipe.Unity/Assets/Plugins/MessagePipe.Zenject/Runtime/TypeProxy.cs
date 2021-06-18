using MessagePipe.Zenject;
using System;
using Zenject;
using Zenject.Internal;

namespace MessagePipe.Zenject
{
    internal struct DiContainerProxy : IServiceCollection
    {
        readonly DiContainer builder;

        public DiContainerProxy(DiContainer builder)
        {
            this.builder = builder;
        }

        public void AddTransient(Type type)
        {
            builder.Bind(type).AsTransient();
        }

        public void TryAddTransient(Type type)
        {
            if (!builder.HasBinding(type))
            {
                builder.Bind(type).AsTransient();
            }
        }

        public void AddSingleton<T>(T instance)
        {
            builder.BindInstance(instance).AsSingle();
        }

        public void AddSingleton(Type type)
        {
            builder.Bind(type).AsSingle();
        }

        public void Add(Type type, InstanceLifetime lifetime)
        {
            var binder = builder.Bind(type);
            SetScope(binder, lifetime);
        }

        public void Add(Type serviceType, Type implementationType, InstanceLifetime lifetime)
        {
            var binder = builder.Bind(serviceType).To(implementationType);
            SetScope(binder, lifetime);
        }

        private void SetScope(ScopeConcreteIdArgConditionCopyNonLazyBinder binder, InstanceLifetime lifetime)
        {
            if (lifetime == InstanceLifetime.Transient)
            {
                binder.AsTransient();
            }
            else
            {
                binder.AsCached();
            }
        }
    }

    [Preserve]
    public sealed class DiContainerProviderProxy : IServiceProvider
    {
        DiContainer container;

        [Preserve]
        public DiContainerProviderProxy(DiContainer container)
        {
            this.container = container;
        }

        public object GetService(Type serviceType)
        {
            return container.Resolve(serviceType);
        }
    }
}

namespace MessagePipe
{
    public static partial class DiContainerExtensions
    {
        public static IServiceProvider AsServiceProvider(this DiContainer container)
        {
            return new DiContainerProviderProxy(container);
        }
    }
}

using System;

namespace MessagePipe
{
    public static class DependencyInjectionShims
    {
        public static T GetRequiredService<T>(this IServiceProvider provider)
        {
            var service = provider.GetService(typeof(T));
            if (service == null)
            {
                throw new InvalidOperationException($"{typeof(T).FullName} is not registered.");
            }
            return (T)service;
        }

        public static object GetRequiredService(this IServiceProvider provider, Type type)
        {
            var service = provider.GetService(type);
            if (service == null)
            {
                throw new InvalidOperationException($"{type.FullName} is not registered.");
            }
            return service;
        }
    }

    public interface IServiceCollection
    {
        void Add(Type serviceType, Type implementationType, InstanceLifetime lifetime);
        void AddSingleton<T>(T instance);
        void AddSingleton(Type type);
        void AddTransient(Type type);
        void TryAddTransient(Type type);
    }
}
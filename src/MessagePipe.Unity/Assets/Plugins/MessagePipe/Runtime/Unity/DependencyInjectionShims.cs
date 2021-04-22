using System;

namespace MessagePipe
{
    internal static class DependencyInjectionShims
    {
        internal static T GetRequiredService<T>(this IServiceProvider provider)
        {
            var service = provider.GetService(typeof(T));
            if (service == null)
            {
                throw new InvalidOperationException($"{typeof(T).FullName} is not registered.");
            }
            return (T)service;
        }

        internal static object GetRequiredService(this IServiceProvider provider, Type type)
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
        public void AddSingleton<T>(T instance);
        public void AddSingleton(Type type);
        public void TryAddTransient(Type type);
    }
}

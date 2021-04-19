#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
#endif
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace MessagePipe
{
    // not intended to use directly, use FilterAttachedMessageHandlerFactory.

    public sealed class FilterCache<TAttribute>
        where TAttribute : IMessagePipeFilterAttribute
    {
        // cache per handler type
        // [MessagePipeFilter(typeof(FilterType))] // FilterType -> cache value
        // public class **FooHandler** : IMessageHandler // cache key

        readonly ConcurrentDictionary<Type, IMessagePipeFilter[]> cache = new ConcurrentDictionary<Type, IMessagePipeFilter[]>();


        public IMessagePipeFilter[] GetOrAddFilters(Type handlerType, IServiceProvider provider)
        {
            if (cache.TryGetValue(handlerType, out var value))
            {
                return value;
            }

            var filterAttributes = handlerType.GetCustomAttributes(typeof(TAttribute), true);

            if (filterAttributes.Length == 0)
            {
                return cache.GetOrAdd(handlerType, Array.Empty<IMessagePipeFilter>());
            }
            else
            {
                var array = filterAttributes.Cast<TAttribute>()
                    .Select(x =>
                    {
                        var f = (IMessagePipeFilter)provider.GetRequiredService(x.Type);
                        f.Order = x.Order;
                        return f;
                    })
                    .ToArray();

                return cache.GetOrAdd(handlerType, array);
            }
        }
    }
}
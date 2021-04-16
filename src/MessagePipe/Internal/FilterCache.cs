using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace MessagePipe.Internal
{
    internal static class FilterCache<TAttribute, TFilter>
        where TAttribute : IMessagePipeAttribute
        where TFilter : IMessagePipeFilter
    {
        static readonly ConcurrentDictionary<Type, FilterAndOrder<TFilter>[]> cache = new ConcurrentDictionary<Type, FilterAndOrder<TFilter>[]>();

        public static FilterAndOrder<TFilter>[] GetOrAddFilters(Type handlerType, IServiceProvider provider)
        {
            if (cache.TryGetValue(handlerType, out var value))
            {
                return value;
            }

            var filterAttributes = handlerType.GetCustomAttributes(typeof(TAttribute), true);

            if (filterAttributes.Length == 0)
            {
                return cache.GetOrAdd(handlerType, Array.Empty<FilterAndOrder<TFilter>>());
            }
            else
            {
                var array = filterAttributes.Cast<TAttribute>()
                    .Select(x => new FilterAndOrder<TFilter>((TFilter)provider.GetRequiredService(x.Type), x.Order))
                    .ToArray();

                return cache.GetOrAdd(handlerType, array);
            }
        }
    }
}

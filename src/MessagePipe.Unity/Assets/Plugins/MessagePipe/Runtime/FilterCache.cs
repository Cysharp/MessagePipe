#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
#endif
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace MessagePipe
{
    // not intended to use directly, use FilterAttachedMessageHandlerFactory.

    public sealed class FilterCache<TAttribute, TFilter>
        where TAttribute : IMessagePipeFilterAttribute
        where TFilter : IMessagePipeFilter
    {
        readonly ConcurrentDictionary<Type, TFilter[]> cache = new ConcurrentDictionary<Type, TFilter[]>();

        public TFilter[] GetOrAddFilters(Type handlerType, IServiceProvider provider)
        {
            if (cache.TryGetValue(handlerType, out var value))
            {
                return value;
            }

            var filterAttributes = handlerType.GetCustomAttributes(typeof(TAttribute), true);

            if (filterAttributes.Length == 0)
            {
                return cache.GetOrAdd(handlerType, Array.Empty<TFilter>());
            }
            else
            {
                var array = filterAttributes.Cast<TAttribute>()
                    .Select(x =>
                    {
                        var f = (TFilter)provider.GetRequiredService(x.Type);
                        f.Order = x.Order;
                        return f;
                    })
                    .ToArray();

                return cache.GetOrAdd(handlerType, array);
            }
        }
    }
}
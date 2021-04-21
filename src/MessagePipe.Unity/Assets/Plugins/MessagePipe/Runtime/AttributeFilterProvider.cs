#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MessagePipe
{
    // not intended to use directly, use FilterAttachedMessageHandlerFactory.

    public sealed class AttributeFilterProvider<TAttribute>
        where TAttribute : IMessagePipeFilterAttribute
    {
        // cache attribute defines.
        readonly ConcurrentDictionary<Type, FilterDefinition[]> cache = new ConcurrentDictionary<Type, FilterDefinition[]>();

        public (int, IEnumerable<IMessagePipeFilter>) GetAttributeFilters(Type handlerType, IServiceProvider provider)
        {
            if (cache.TryGetValue(handlerType, out var value))
            {
                if (value.Length == 0) return (0, Array.Empty<IMessagePipeFilter>());
                return (value.Length, CreateFilters(value, provider));
            }

            // require to get all filter for alidate.
            var filterAttributes = handlerType.GetCustomAttributes(typeof(IMessagePipeFilterAttribute), true).OfType<TAttribute>().ToArray();
            if (filterAttributes.Length == 0)
            {
                cache[handlerType] = Array.Empty<FilterDefinition>();
                return (0, Array.Empty<IMessagePipeFilter>());
            }
            else
            {
                var array = filterAttributes.Cast<TAttribute>().Select(x => new FilterDefinition(x.Type, x.Order)).ToArray();
                var filterDefinitions = cache.GetOrAdd(handlerType, array);
                return (filterDefinitions.Length, CreateFilters(filterDefinitions, provider));
            }
        }

        static IEnumerable<IMessagePipeFilter> CreateFilters(FilterDefinition[] filterDefinitions, IServiceProvider provider)
        {
            foreach (var filterDefinition in filterDefinitions)
            {
                var f = (IMessagePipeFilter)provider.GetRequiredService(filterDefinition.FilterType);
                f.Order = filterDefinition.Order;
                yield return f;
            }
        }
    }
}
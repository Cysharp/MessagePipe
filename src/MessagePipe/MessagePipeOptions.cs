using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MessagePipe
{
    public enum AsyncPublishStrategy
    {
        Sequential,
        Parallel
    }

    public enum DefaultHandlerRepository
    {
        /// <summary>Register handler to immutable-array, best for publish performance.</summary>
        ImmutableArray,
        /// <summary>Register handler to concurrent-dictionary, best for add/remove peformance.</summary>
        ConcurrentDictionary
    }

    public sealed class MessagePipeOptions
    {
        public AsyncPublishStrategy DefaultAsyncPublishStrategy { get; set; }
        public DefaultHandlerRepository DefaultHandlerRepository { get; set; }

        // TODO:Assembly?
        public bool AutowireRequestHandler { get; set; }

        /// <summary>For diagnostics usage, enable MessagePipeDiagnosticsInfo.CapturedStacktraces.</summary>
        public bool EnableCaptureStackTrace { get; set; }

        public MessagePipeOptions()
        {
            this.DefaultAsyncPublishStrategy = AsyncPublishStrategy.Parallel;
            this.DefaultHandlerRepository = DefaultHandlerRepository.ImmutableArray;
        }



        // Filters

        internal void AddGlobalFilter(IServiceCollection services)
        {
            foreach (var item in messageHandlerFilters)
            {
                services.AddSingleton(item.FilterType);
            }

            // TODO:other filters.
        }


        List<FilterTypeAndOrder> messageHandlerFilters = new List<FilterTypeAndOrder>();
        FilterAndOrder<MessageHandlerFilter>[]? messageHandlerFilterCache;

        public void AddGlobalMessageHandlerFilter<T>(int order = 0)
            where T : MessageHandlerFilter
        {
            messageHandlerFilters.Add(new FilterTypeAndOrder(typeof(T), order));
        }

        internal FilterAndOrder<MessageHandlerFilter>[] GetGlobalMessageHandlerFilters(IServiceProvider provider)
        {
            return GetOrCreateHandlerCache(ref messageHandlerFilters, ref messageHandlerFilterCache, provider);
        }

        static FilterAndOrder<T>[] GetOrCreateHandlerCache<T>(ref List<FilterTypeAndOrder> filterDefinitions, ref FilterAndOrder<T>[]? filterCache, IServiceProvider provider)
            where T : IMessagePipeFilter
        {
            if (filterCache == null)
            {
                lock (filterDefinitions)
                {
                    if (filterCache == null)
                    {
                        var temp = new FilterAndOrder<T>[filterDefinitions.Count];
                        for (int i = 0; i < temp.Length; i++)
                        {
                            var filter = provider.GetRequiredService(filterDefinitions[i].FilterType);
                            temp[i] = new FilterAndOrder<T>((T)filter, filterDefinitions[i].Order);
                        }
                        filterCache = temp;
                    }
                }
            }

            return filterCache;
        }
    }


}
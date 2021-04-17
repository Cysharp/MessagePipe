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

    internal readonly struct FilterTypeAndOrder
    {
        public readonly Type FilterType;
        public readonly int Order;

        public FilterTypeAndOrder(Type filterType, int order)
        {
            FilterType = filterType;
            Order = order;
        }
    }

    public sealed class MessagePipeOptions
    {
        public AsyncPublishStrategy DefaultAsyncPublishStrategy { get; set; }

        // TODO:Assembly?
        public bool AutowireRequestHandler { get; set; }

        /// <summary>For diagnostics usage, enable MessagePipeDiagnosticsInfo.CapturedStacktraces.</summary>
        public bool EnableCaptureStackTrace { get; set; }

        public MessagePipeOptions()
        {
            this.DefaultAsyncPublishStrategy = AsyncPublishStrategy.Parallel;
        }



        // Filters

        // register DI
        internal void AddGlobalFilter(IServiceCollection services)
        {
            foreach (var item in messageHandlerFilters)
            {
                services.AddSingleton(item.FilterType);
            }

            foreach (var item in asyncMessageHandlerFilters)
            {
                services.AddSingleton(item.FilterType);
            }
        }

        List<FilterTypeAndOrder> messageHandlerFilters = new List<FilterTypeAndOrder>();
        MessageHandlerFilter[]? messageHandlerFilterCache;

        public void AddGlobalMessageHandlerFilter<T>(int order = 0)
            where T : MessageHandlerFilter
        {
            messageHandlerFilters.Add(new FilterTypeAndOrder(typeof(T), order));
        }

        internal MessageHandlerFilter[] GetGlobalMessageHandlerFilters(IServiceProvider provider)
        {
            return GetOrCreateHandlerCache(ref messageHandlerFilters, ref messageHandlerFilterCache, provider);
        }

        List<FilterTypeAndOrder> asyncMessageHandlerFilters = new List<FilterTypeAndOrder>();
        AsyncMessageHandlerFilter[]? asyncMessageHandlerFilterCache;

        public void AddGlobalAsyncMessageHandlerFilter<T>(int order = 0)
            where T : AsyncMessageHandlerFilter
        {
            asyncMessageHandlerFilters.Add(new FilterTypeAndOrder(typeof(T), order));
        }

        internal AsyncMessageHandlerFilter[] GetGlobalAsyncMessageHandlerFilters(IServiceProvider provider)
        {
            return GetOrCreateHandlerCache(ref asyncMessageHandlerFilters, ref asyncMessageHandlerFilterCache, provider);
        }

        static T[] GetOrCreateHandlerCache<T>(ref List<FilterTypeAndOrder> filterDefinitions, ref T[]? filterCache, IServiceProvider provider)
            where T : IMessagePipeFilter
        {
            if (filterCache == null)
            {
                lock (filterDefinitions)
                {
                    if (filterCache == null)
                    {
                        var temp = new T[filterDefinitions.Count];
                        for (int i = 0; i < temp.Length; i++)
                        {
                            var filter = (T)provider.GetRequiredService(filterDefinitions[i].FilterType);
                            filter.Order = filterDefinitions[i].Order;
                            temp[i] = filter;
                        }
                        filterCache = temp;
                    }
                }
            }

            return filterCache;
        }
    }


}
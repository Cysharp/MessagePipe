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

    public enum InstanceScope
    {
        Singleton, Scoped
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
        /// <summary>PublishAsync</summary>
        public AsyncPublishStrategy DefaultAsyncPublishStrategy { get; set; }

        public bool EnableAutowire { get; set; }

        /// <summary>For diagnostics usage, enable MessagePipeDiagnosticsInfo.CapturedStacktraces.</summary>
        public bool EnableCaptureStackTrace { get; set; }

        public InstanceScope InstanceScope { get; set; }

        public MessagePipeOptions()
        {
            this.DefaultAsyncPublishStrategy = AsyncPublishStrategy.Parallel;
            this.InstanceScope = InstanceScope.Singleton;
            this.EnableAutowire = true;
            this.EnableCaptureStackTrace = false;
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

            foreach (var item in requestHandlerFilters)
            {
                services.AddSingleton(item.FilterType);
            }

            foreach (var item in asyncRequestHandlerFilters)
            {
                services.AddSingleton(item.FilterType);
            }
        }

        // MessageHandlerFilter

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

        // AsyncMessageHandlerFilter

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

        // RequestHandlerFilter

        List<FilterTypeAndOrder> requestHandlerFilters = new List<FilterTypeAndOrder>();
        RequestHandlerFilter[]? requestHandlerFilterCache;

        public void AddGlobalRequestHandlerFilter<T>(int order = 0)
            where T : RequestHandlerFilter
        {
            requestHandlerFilters.Add(new FilterTypeAndOrder(typeof(T), order));
        }

        internal RequestHandlerFilter[] GetGlobalRequestHandlerFilters(IServiceProvider provider)
        {
            return GetOrCreateHandlerCache(ref requestHandlerFilters, ref requestHandlerFilterCache, provider);
        }

        //  AsyncRequestHandlerFilter

        List<FilterTypeAndOrder> asyncRequestHandlerFilters = new List<FilterTypeAndOrder>();
        AsyncRequestHandlerFilter[]? asyncRequestHandlerFilterCache;

        public void AddGlobalAsyncRequestHandlerFilter<T>(int order = 0)
            where T : RequestHandlerFilter
        {
            asyncRequestHandlerFilters.Add(new FilterTypeAndOrder(typeof(T), order));
        }

        internal AsyncRequestHandlerFilter[] GetGlobalAsyncRequestHandlerFilters(IServiceProvider provider)
        {
            return GetOrCreateHandlerCache(ref asyncRequestHandlerFilters, ref asyncRequestHandlerFilterCache, provider);
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
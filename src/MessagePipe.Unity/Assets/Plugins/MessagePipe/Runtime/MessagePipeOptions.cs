#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MessagePipe
{
    public enum AsyncPublishStrategy
    {
        Sequential,
        Parallel
    }

    public enum InstanceLifetime
    {
        Singleton, Scoped
    }

    public enum HandlingSubscribeDisposedPolicy
    {
        Ignore, Throw
    }

    internal static class HandlingSubscribeDisposedPolicyExtensions
    {
        public static IDisposable Handle(this HandlingSubscribeDisposedPolicy policy, string name)
        {
            if (policy == HandlingSubscribeDisposedPolicy.Throw)
            {
                throw new ObjectDisposedException(name);
            }
            return DisposableBag.Empty;
        }
    }

    internal readonly struct FilterDefinition
    {
        public readonly Type FilterType;
        public readonly int Order;

        public FilterDefinition(Type filterType, int order)
        {
            FilterType = filterType;
            Order = order;
        }
    }

    public sealed class MessagePipeOptions
    {
        /// <summary>PublishAsync</summary>
        public AsyncPublishStrategy DefaultAsyncPublishStrategy { get; set; }

#if !UNITY_2018_3_OR_NEWER
        public bool EnableAutoRegistration { get; set; }

#endif

        /// <summary>For diagnostics usage, enable MessagePipeDiagnosticsInfo.CapturedStacktraces; default is false.</summary>
        public bool EnableCaptureStackTrace { get; set; }

        public HandlingSubscribeDisposedPolicy HandlingSubscribeDisposedPolicy { get; set; }

        public InstanceLifetime InstanceLifetime { get; set; }

        public MessagePipeOptions()
        {
            this.DefaultAsyncPublishStrategy = AsyncPublishStrategy.Parallel;
            this.InstanceLifetime = InstanceLifetime.Singleton;
            this.EnableCaptureStackTrace = false;
            this.HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore;
#if !UNITY_2018_3_OR_NEWER
            this.EnableAutoRegistration = true;
#endif
        }

#if !UNITY_2018_3_OR_NEWER

        // autowire

        internal Assembly[] autoregistrationAssemblies;
        internal Type[] autoregistrationTypes;

        public void SetAutoRegistrationSearchAssemblies(params Assembly[] assemblies)
        {
            autoregistrationAssemblies = assemblies;
        }

        public void SetAutoRegistrationSearchTypes(params Type[] types)
        {
            autoregistrationTypes = types;
        }

        internal void AddGlobalFilter(IServiceCollection services)
        {
            // all filters should register as transient.
            foreach (var item in messageHandlerFilters)
            {
                services.TryAddTransient(item.FilterType);
            }

            foreach (var item in asyncMessageHandlerFilters)
            {
                services.TryAddTransient(item.FilterType);
            }

            foreach (var item in requestHandlerFilters)
            {
                services.TryAddTransient(item.FilterType);
            }

            foreach (var item in asyncRequestHandlerFilters)
            {
                services.TryAddTransient(item.FilterType);
            }
        }

#endif

        // MessageHandlerFilter

        List<FilterDefinition> messageHandlerFilters = new List<FilterDefinition>();

        public void AddGlobalMessageHandlerFilter<T>(int order = 0)
            where T : IMessageHandlerFilter
        {
            messageHandlerFilters.Add(new FilterDefinition(typeof(T), order));
        }

        internal (int count, IEnumerable<IMessageHandlerFilter>) GetGlobalMessageHandlerFilters(IServiceProvider provider)
        {
            return (messageHandlerFilters.Count, CreateFilters<IMessageHandlerFilter>(messageHandlerFilters, provider));
        }

        // AsyncMessageHandlerFilter

        List<FilterDefinition> asyncMessageHandlerFilters = new List<FilterDefinition>();

        public void AddGlobalAsyncMessageHandlerFilter<T>(int order = 0)
            where T : IAsyncMessageHandlerFilter
        {
            asyncMessageHandlerFilters.Add(new FilterDefinition(typeof(T), order));
        }

        internal (int count, IEnumerable<IAsyncMessageHandlerFilter>) GetGlobalAsyncMessageHandlerFilters(IServiceProvider provider)
        {
            return (asyncMessageHandlerFilters.Count, CreateFilters<IAsyncMessageHandlerFilter>(asyncMessageHandlerFilters, provider));
        }

        // RequestHandlerFilter

        List<FilterDefinition> requestHandlerFilters = new List<FilterDefinition>();

        public void AddGlobalRequestHandlerFilter<T>(int order = 0)
            where T : IRequestHandlerFilter
        {
            requestHandlerFilters.Add(new FilterDefinition(typeof(T), order));
        }

        internal (int, IEnumerable<IRequestHandlerFilter>) GetGlobalRequestHandlerFilters(IServiceProvider provider)
        {
            return (requestHandlerFilters.Count, CreateFilters<IRequestHandlerFilter>(requestHandlerFilters, provider));
        }

        //  AsyncRequestHandlerFilter

        List<FilterDefinition> asyncRequestHandlerFilters = new List<FilterDefinition>();

        public void AddGlobalAsyncRequestHandlerFilter<T>(int order = 0)
            where T : IAsyncRequestHandlerFilter
        {
            asyncRequestHandlerFilters.Add(new FilterDefinition(typeof(T), order));
        }

        internal (int, IEnumerable<IAsyncRequestHandlerFilter>) GetGlobalAsyncRequestHandlerFilters(IServiceProvider provider)
        {
            return (asyncRequestHandlerFilters.Count, CreateFilters<IAsyncRequestHandlerFilter>(asyncRequestHandlerFilters, provider));
        }

        static IEnumerable<T> CreateFilters<T>(List<FilterDefinition> filterDefinitions, IServiceProvider provider)
            where T : IMessagePipeFilter
        {
            if (filterDefinitions.Count == 0) return Array.Empty<T>();
            return CreateFiltersCore<T>(filterDefinitions, provider);
        }

        static IEnumerable<T> CreateFiltersCore<T>(List<FilterDefinition> filterDefinitions, IServiceProvider provider)
            where T : IMessagePipeFilter
        {
            for (int i = 0; i < filterDefinitions.Count; i++)
            {
                var filter = (T)provider.GetRequiredService(filterDefinitions[i].FilterType);
                filter.Order = filterDefinitions[i].Order;
                yield return filter;
            }
        }
    }
}
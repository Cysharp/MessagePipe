#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
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
        /// <summary>if null, open generics.</summary>
        public readonly Type MessageType;

        public FilterDefinition(Type filterType, int order)
        {
            FilterType = filterType;
            Order = order;
            if (!filterType.IsConstructedGenericType)
            {
                MessageType = null;
            }
            else
            {
                MessageType = filterType.GetGenericArguments()[0];
            }
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

        // auto-registration

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

#endif

        // filters

        internal IEnumerable<Type> GetGlobalFilterTypes()
        {
            foreach (var item in messageHandlerFilters)
            {
                yield return item.FilterType;
            }

            foreach (var item in asyncMessageHandlerFilters)
            {
                yield return item.FilterType;
            }

            foreach (var item in requestHandlerFilters)
            {
                yield return item.FilterType;
            }

            foreach (var item in asyncRequestHandlerFilters)
            {
                yield return item.FilterType;
            }
        }

        // MessageHandlerFilter

        List<FilterDefinition> messageHandlerFilters = new List<FilterDefinition>();

        /// <summary>
        /// If register open generics(typeof(MyFilter&lt;&gt;)) to register all message types.
        /// </summary>
        public void AddGlobalMessageHandlerFilter(Type type, int order = 0)
        {
            ValidateFilterType(type, typeof(IMessageHandlerFilter));
            messageHandlerFilters.Add(new FilterDefinition(type, order));
        }

        public void AddGlobalMessageHandlerFilter<T>(int order = 0)
            where T : IMessageHandlerFilter
        {
            messageHandlerFilters.Add(new FilterDefinition(typeof(T), order));
        }

        internal (int count, IEnumerable<IMessageHandlerFilter>) GetGlobalMessageHandlerFilters(IServiceProvider provider, Type messageType)
        {
            return (messageHandlerFilters.Count, CreateFilters<IMessageHandlerFilter>(messageHandlerFilters, provider, messageType));
        }

        // AsyncMessageHandlerFilter

        List<FilterDefinition> asyncMessageHandlerFilters = new List<FilterDefinition>();

        /// <summary>
        /// If register open generics(typeof(MyFilter&lt;&gt;)) to register all message types.
        /// </summary>
        public void AddGlobalAsyncMessageHandlerFilter(Type type, int order = 0)
        {
            ValidateFilterType(type, typeof(IAsyncMessageHandlerFilter));
            asyncMessageHandlerFilters.Add(new FilterDefinition(type, order));
        }

        public void AddGlobalAsyncMessageHandlerFilter<T>(int order = 0)
            where T : IAsyncMessageHandlerFilter
        {
            asyncMessageHandlerFilters.Add(new FilterDefinition(typeof(T), order));
        }

        internal (int count, IEnumerable<IAsyncMessageHandlerFilter>) GetGlobalAsyncMessageHandlerFilters(IServiceProvider provider, Type messageType)
        {
            return (asyncMessageHandlerFilters.Count, CreateFilters<IAsyncMessageHandlerFilter>(asyncMessageHandlerFilters, provider, messageType));
        }

        // RequestHandlerFilter

        List<FilterDefinition> requestHandlerFilters = new List<FilterDefinition>();

        /// <summary>
        /// If register open generics(typeof(MyFilter&lt;&gt;)) to register all message types.
        /// </summary>
        public void AddGlobalRequestHandlerFilter(Type type, int order = 0)
        {
            ValidateFilterType(type, typeof(IRequestHandlerFilter));
            requestHandlerFilters.Add(new FilterDefinition(type, order));
        }

        public void AddGlobalRequestHandlerFilter<T>(int order = 0)
            where T : IRequestHandlerFilter
        {
            requestHandlerFilters.Add(new FilterDefinition(typeof(T), order));
        }

        internal (int, IEnumerable<IRequestHandlerFilter>) GetGlobalRequestHandlerFilters(IServiceProvider provider, Type requestType)
        {
            return (requestHandlerFilters.Count, CreateFilters<IRequestHandlerFilter>(requestHandlerFilters, provider, requestType));
        }

        //  AsyncRequestHandlerFilter

        List<FilterDefinition> asyncRequestHandlerFilters = new List<FilterDefinition>();

        /// <summary>
        /// If register open generics(typeof(MyFilter&lt;&gt;)) to register all message types.
        /// </summary>
        public void AddGlobalAsyncRequestHandlerFilter(Type type, int order = 0)
        {
            ValidateFilterType(type, typeof(IAsyncRequestHandlerFilter));
            asyncRequestHandlerFilters.Add(new FilterDefinition(type, order));
        }

        public void AddGlobalAsyncRequestHandlerFilter<T>(int order = 0)
            where T : IAsyncRequestHandlerFilter
        {
            asyncRequestHandlerFilters.Add(new FilterDefinition(typeof(T), order));
        }

        internal (int, IEnumerable<IAsyncRequestHandlerFilter>) GetGlobalAsyncRequestHandlerFilters(IServiceProvider provider, Type requestType)
        {
            return (asyncRequestHandlerFilters.Count, CreateFilters<IAsyncRequestHandlerFilter>(asyncRequestHandlerFilters, provider, requestType));
        }

        static IEnumerable<T> CreateFilters<T>(List<FilterDefinition> filterDefinitions, IServiceProvider provider, Type messageType)
            where T : IMessagePipeFilter
        {
            if (filterDefinitions.Count == 0) return Array.Empty<T>();
            return CreateFiltersCore<T>(filterDefinitions, provider, messageType);
        }

        static IEnumerable<T> CreateFiltersCore<T>(List<FilterDefinition> filterDefinitions, IServiceProvider provider, Type messageType)
            where T : IMessagePipeFilter
        {
            for (int i = 0; i < filterDefinitions.Count; i++)
            {
                var def = filterDefinitions[i];
                var filterType = def.FilterType;
                if (def.MessageType == null)
                {
                    filterType = filterType.MakeGenericType(messageType);
                }
                else if (def.MessageType != messageType)
                {
                    continue;
                }

                var filter = (T)provider.GetRequiredService(filterType);
                filter.Order = def.Order;
                yield return filter;
            }
        }

        void ValidateFilterType(Type type, Type filterType)
        {
            if (!filterType.IsAssignableFrom(type))
            {
                throw new ArgumentException($"{type.FullName} is not {filterType.Name}");
            }
        }
    }
}
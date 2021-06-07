using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MessagePipe.Internal;
#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
#endif
namespace MessagePipe
{
    public enum AsyncPublishStrategy
    {
        Parallel, Sequential
    }

    public enum InstanceLifetime
    {
        Singleton, Scoped, Transient
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

    public sealed class MessagePipeOptions
    {
        /// <summary>AsyncPublisher.PublishAsync's concurrent starategy, default is Parallel.</summary>
        public AsyncPublishStrategy DefaultAsyncPublishStrategy { get; set; }

#if !UNITY_2018_3_OR_NEWER
        public bool EnableAutoRegistration { get; set; }

#endif

        /// <summary>For diagnostics usage, enable MessagePipeDiagnosticsInfo.CapturedStacktraces; default is false.</summary>
        public bool EnableCaptureStackTrace { get; set; }

        /// <summary>Choose how work on subscriber.Subscribe when after disposed, default is Ignore.</summary>
        public HandlingSubscribeDisposedPolicy HandlingSubscribeDisposedPolicy { get; set; }

        /// <summary>Default publisher/subscribe's lifetime scope, default is Singleton.</summary>
        public InstanceLifetime InstanceLifetime { get; set; }

        /// <summary>Default IRequestHandler/IAsyncRequestHandler's lifetime scope, default is Scoped.</summary>
        public InstanceLifetime RequestHandlerLifetime { get; set; }

        public MessagePipeOptions()
        {
            this.DefaultAsyncPublishStrategy = AsyncPublishStrategy.Parallel;
            this.InstanceLifetime = InstanceLifetime.Singleton;
            this.RequestHandlerLifetime = InstanceLifetime.Scoped;
            this.EnableCaptureStackTrace = false;
            this.HandlingSubscribeDisposedPolicy = HandlingSubscribeDisposedPolicy.Ignore;
#if !UNITY_2018_3_OR_NEWER
            this.EnableAutoRegistration = true;
            this.autoregistrationAssemblies = null;
            this.autoregistrationTypes = null;
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

#if !UNITY_2018_3_OR_NEWER

        /// <summary>
        /// If register open generics(typeof(MyFilter&lt;&gt;)) to register all message types.
        /// </summary>
        public void AddGlobalMessageHandlerFilter(Type type, int order = 0)
        {
            ValidateFilterType(type, typeof(IMessageHandlerFilter));
            messageHandlerFilters.Add(new MessageHandlerFilterDefinition(type, order, typeof(MessageHandlerFilter<>)));
        }

#endif

        public void AddGlobalMessageHandlerFilter<T>(int order = 0)
            where T : IMessageHandlerFilter
        {
            messageHandlerFilters.Add(new MessageHandlerFilterDefinition(typeof(T), order, typeof(MessageHandlerFilter<>)));
        }

        internal (int count, IEnumerable<IMessageHandlerFilter>) GetGlobalMessageHandlerFilters(IServiceProvider provider, Type messageType)
        {
            return (messageHandlerFilters.Count, CreateFilters<IMessageHandlerFilter>(messageHandlerFilters, provider, messageType));
        }

        // AsyncMessageHandlerFilter

        List<FilterDefinition> asyncMessageHandlerFilters = new List<FilterDefinition>();

#if !UNITY_2018_3_OR_NEWER

        /// <summary>
        /// If register open generics(typeof(MyFilter&lt;&gt;)) to register all message types.
        /// </summary>
        public void AddGlobalAsyncMessageHandlerFilter(Type type, int order = 0)
        {
            ValidateFilterType(type, typeof(IAsyncMessageHandlerFilter));
            asyncMessageHandlerFilters.Add(new MessageHandlerFilterDefinition(type, order, typeof(AsyncMessageHandlerFilter<>)));
        }

#endif

        public void AddGlobalAsyncMessageHandlerFilter<T>(int order = 0)
            where T : IAsyncMessageHandlerFilter
        {
            asyncMessageHandlerFilters.Add(new MessageHandlerFilterDefinition(typeof(T), order, typeof(AsyncMessageHandlerFilter<>)));
        }

        internal (int count, IEnumerable<IAsyncMessageHandlerFilter>) GetGlobalAsyncMessageHandlerFilters(IServiceProvider provider, Type messageType)
        {
            return (asyncMessageHandlerFilters.Count, CreateFilters<IAsyncMessageHandlerFilter>(asyncMessageHandlerFilters, provider, messageType));
        }

        // RequestHandlerFilter

        List<FilterDefinition> requestHandlerFilters = new List<FilterDefinition>();

#if !UNITY_2018_3_OR_NEWER

        /// <summary>
        /// If register open generics(typeof(MyFilter&lt;&gt;)) to register all message types.
        /// </summary>
        public void AddGlobalRequestHandlerFilter(Type type, int order = 0)
        {
            ValidateFilterType(type, typeof(IRequestHandlerFilter));
            requestHandlerFilters.Add(new RequestHandlerFilterDefinition(type, order, typeof(RequestHandlerFilter<,>)));
        }

#endif

        public void AddGlobalRequestHandlerFilter<T>(int order = 0)
            where T : IRequestHandlerFilter
        {
            requestHandlerFilters.Add(new RequestHandlerFilterDefinition(typeof(T), order, typeof(RequestHandlerFilter<,>)));
        }

        internal (int, IEnumerable<IRequestHandlerFilter>) GetGlobalRequestHandlerFilters(IServiceProvider provider, Type requestType, Type responseType)
        {
            return (requestHandlerFilters.Count, CreateFilters<IRequestHandlerFilter>(requestHandlerFilters, provider, requestType, responseType));
        }

        //  AsyncRequestHandlerFilter

        List<FilterDefinition> asyncRequestHandlerFilters = new List<FilterDefinition>();

#if !UNITY_2018_3_OR_NEWER

        /// <summary>
        /// If register open generics(typeof(MyFilter&lt;&gt;)) to register all message types.
        /// </summary>
        public void AddGlobalAsyncRequestHandlerFilter(Type type, int order = 0)
        {
            ValidateFilterType(type, typeof(IAsyncRequestHandlerFilter));
            asyncRequestHandlerFilters.Add(new RequestHandlerFilterDefinition(type, order, typeof(AsyncRequestHandlerFilter<,>)));
        }

#endif

        public void AddGlobalAsyncRequestHandlerFilter<T>(int order = 0)
            where T : IAsyncRequestHandlerFilter
        {
            asyncRequestHandlerFilters.Add(new RequestHandlerFilterDefinition(typeof(T), order, typeof(AsyncRequestHandlerFilter<,>)));
        }

        internal (int, IEnumerable<IAsyncRequestHandlerFilter>) GetGlobalAsyncRequestHandlerFilters(IServiceProvider provider, Type requestType, Type responseType)
        {
            return (asyncRequestHandlerFilters.Count, CreateFilters<IAsyncRequestHandlerFilter>(asyncRequestHandlerFilters, provider, requestType, responseType));
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
                var def = filterDefinitions[i] as MessageHandlerFilterDefinition;
                if (def == null) continue;
                var filterType = def.FilterType;
                if (def.IsOpenGenerics)
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

        static IEnumerable<T> CreateFilters<T>(List<FilterDefinition> filterDefinitions, IServiceProvider provider, Type requestType, Type responseType)
            where T : IMessagePipeFilter
        {
            if (filterDefinitions.Count == 0) return Array.Empty<T>();
            return CreateFiltersCore<T>(filterDefinitions, provider, requestType, responseType);
        }

        static IEnumerable<T> CreateFiltersCore<T>(List<FilterDefinition> filterDefinitions, IServiceProvider provider, Type requestType, Type responseType)
            where T : IMessagePipeFilter
        {
            for (int i = 0; i < filterDefinitions.Count; i++)
            {
                var def = filterDefinitions[i] as RequestHandlerFilterDefinition;
                if (def == null) continue;
                var filterType = def.FilterType;
                if (def.IsOpenGenerics)
                {
                    filterType = filterType.MakeGenericType(requestType, responseType);
                }
                else if (!(def.RequestType == requestType && def.ResponseType == responseType))
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
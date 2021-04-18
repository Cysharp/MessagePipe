using MessagePipe.Internal;
using System;

namespace MessagePipe
{
    public sealed class FilterAttachedMessageHandlerFactory
    {
        readonly MessagePipeOptions options;
        readonly FilterCache<MessageHandlerFilterAttribute, MessageHandlerFilter> filterCache;
        readonly IServiceProvider provider;

        public FilterAttachedMessageHandlerFactory(MessagePipeOptions options, FilterCache<MessageHandlerFilterAttribute, MessageHandlerFilter> filterCache, IServiceProvider provider)
        {
            this.options = options;
            this.filterCache = filterCache;
            this.provider = provider;
        }

        public IMessageHandler<TMessage> CreateMessageHandler<TMessage>(IMessageHandler<TMessage> handler, MessageHandlerFilter[] filters)
        {
            var handlerFilters = filterCache.GetOrAddFilters(handler.GetType(), provider);
            var globalFilters = options.GetGlobalMessageHandlerFilters(provider);

            if (filters.Length != 0 || handlerFilters.Length != 0 || globalFilters.Length != 0)
            {
                handler = new FilterAttachedMessageHandler<TMessage>(handler, ArrayUtil.Concat(filters, handlerFilters, globalFilters));
            }

            return handler;
        }
    }

    public sealed class FilterAttachedAsyncMessageHandlerFactory
    {
        readonly MessagePipeOptions options;
        readonly FilterCache<AsyncMessageHandlerFilterAttribute, AsyncMessageHandlerFilter> filterCache;
        readonly IServiceProvider provider;

        public FilterAttachedAsyncMessageHandlerFactory(MessagePipeOptions options, FilterCache<AsyncMessageHandlerFilterAttribute, AsyncMessageHandlerFilter> filterCache, IServiceProvider provider)
        {
            this.options = options;
            this.filterCache = filterCache;
            this.provider = provider;
        }

        public IAsyncMessageHandler<TMessage> CreateAsyncMessageHandler<TMessage>(IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter[] filters)
        {
            var handlerFilters = filterCache.GetOrAddFilters(handler.GetType(), provider);
            var globalFilters = options.GetGlobalAsyncMessageHandlerFilters(provider);

            if (filters.Length != 0 || handlerFilters.Length != 0 || globalFilters.Length != 0)
            {
                handler = new FilterAttachedAsyncMessageHandler<TMessage>(handler, ArrayUtil.Concat(filters, handlerFilters, globalFilters));
            }

            return handler;
        }
    }
}
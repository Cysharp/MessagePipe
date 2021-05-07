using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    // Sync

    [Preserve]
    public sealed class FilterAttachedMessageHandlerFactory
    {
        readonly MessagePipeOptions options;
        readonly AttributeFilterProvider<MessageHandlerFilterAttribute> filterProvider;
        readonly IServiceProvider provider;

        [Preserve]
        public FilterAttachedMessageHandlerFactory(MessagePipeOptions options, AttributeFilterProvider<MessageHandlerFilterAttribute> filterProvider, IServiceProvider provider)
        {
            this.options = options;
            this.filterProvider = filterProvider;
            this.provider = provider;
        }

        public IMessageHandler<TMessage> CreateMessageHandler<TMessage>(IMessageHandler<TMessage> handler, MessageHandlerFilter<TMessage>[] filters)
        {
            var (globalLength, globalFilters) = options.GetGlobalMessageHandlerFilters(provider, typeof(TMessage));
            var (handlerLength, handlerFilters) = filterProvider.GetAttributeFilters(handler.GetType(), provider);

            if (filters.Length != 0 || globalLength != 0 || handlerLength != 0)
            {
                handler = new FilterAttachedMessageHandler<TMessage>(handler, globalFilters.Concat(handlerFilters).Concat(filters).Cast<MessageHandlerFilter<TMessage>>());
            }

            return handler;
        }
    }

    internal sealed class FilterAttachedMessageHandler<T> : IMessageHandler<T>
    {
        Action<T> handler;

        public FilterAttachedMessageHandler(IMessageHandler<T> body, IEnumerable<MessageHandlerFilter<T>> filters)
        {
            Action<T> next = body.Handle;
            foreach (var f in filters.OrderByDescending(x => x.Order))
            {
                next = new MessageHandlerFilterRunner<T>(f, next).GetDelegate();
            }

            this.handler = next;
        }

        public void Handle(T message)
        {
            handler(message);
        }
    }

    internal sealed class MessageHandlerFilterRunner<T>
    {
        readonly MessageHandlerFilter<T> filter;
        readonly Action<T> next;

        public MessageHandlerFilterRunner(MessageHandlerFilter<T> filter, Action<T> next)
        {
            this.filter = filter;
            this.next = next;
        }

        public Action<T> GetDelegate() => Handle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Handle(T message)
        {
            filter.Handle(message, next);
        }
    }
}
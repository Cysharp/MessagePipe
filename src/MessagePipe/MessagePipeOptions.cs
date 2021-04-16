using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace MessagePipe
{
    public enum AsyncPublishStrategy
    {
        Sequential,
        Parallel
    }

    public sealed class MessagePipeOptions
    {
        public AsyncPublishStrategy DefaultAsyncPublishStrategy { get; set; }

        // TODO:AddNantokaFilter???, enable to register per message type? predicate???
        public MessagePipeFilterAttribute[] GlobalMessagePipeFilters { get; set; }

        // TODO:Assembly?
        public bool AutowireRequestHandler { get; set; }

        public MessagePipeOptions()
        {
            GlobalMessagePipeFilters = Array.Empty<MessagePipeFilterAttribute>();
        }

        // Filter caches.

        (MessageHandlerFilter, int)[]? handlerFilters;

        internal (MessageHandlerFilter, int)[] GetRequestHandler(IServiceProvider provider)
        {
            if (handlerFilters == null)
            {
                var list = new List<(MessageHandlerFilter, int)>();
                foreach (var item in GlobalMessagePipeFilters)
                {
                    if (typeof(RequestHandlerFilter).IsAssignableFrom(item.Type))
                    {
                        var filter = (MessageHandlerFilter)provider.GetRequiredService(item.Type);
                        list.Add((filter, item.Order));
                    }
                }

                handlerFilters = list.ToArray();
            }

            return handlerFilters;
        }
    }
}
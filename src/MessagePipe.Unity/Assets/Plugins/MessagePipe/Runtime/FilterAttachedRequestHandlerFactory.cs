using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MessagePipe
{
    // Sync

    [Preserve]
    public sealed class FilterAttachedRequestHandlerFactory
    {
        readonly MessagePipeOptions options;
        readonly AttributeFilterProvider<RequestHandlerFilterAttribute> filterProvider;
        readonly IServiceProvider provider;

        [Preserve]
        public FilterAttachedRequestHandlerFactory(MessagePipeOptions options, AttributeFilterProvider<RequestHandlerFilterAttribute> filterProvider, IServiceProvider provider)
        {
            this.options = options;
            this.filterProvider = filterProvider;
            this.provider = provider;
        }

        public IRequestHandlerCore<TRequest, TResponse> CreateRequestHandler<TRequest, TResponse>(IRequestHandlerCore<TRequest, TResponse> handler)
        {
            var (globalLength, globalFilters) = options.GetGlobalRequestHandlerFilters(provider, typeof(TRequest), typeof(TResponse));
            var (handlerLength, handlerFilters) = filterProvider.GetAttributeFilters(handler.GetType(), provider);

            if (globalLength != 0 || handlerLength != 0)
            {
                handler = new FilterAttachedRequestHandler<TRequest, TResponse>(handler, globalFilters.Concat(handlerFilters).Cast<RequestHandlerFilter<TRequest, TResponse>>());
            }

            return handler;
        }
    }

    internal sealed class FilterAttachedRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    {
        Func<TRequest, TResponse> handler;

        public FilterAttachedRequestHandler(IRequestHandlerCore<TRequest, TResponse> body, IEnumerable<RequestHandlerFilter<TRequest, TResponse>> filters)
        {
            Func<TRequest, TResponse> next = body.Invoke;
            foreach (var f in filters.OrderByDescending(x => x.Order))
            {
                next = new RequestHandlerFilterRunner<TRequest, TResponse>(f, next).GetDelegate();
            }

            this.handler = next;
        }

        public TResponse Invoke(TRequest request)
        {
            return handler(request);
        }
    }

    internal sealed class RequestHandlerFilterRunner<TRequest, TResponse>
    {
        readonly RequestHandlerFilter<TRequest, TResponse> filter;
        readonly Func<TRequest, TResponse> next;

        public RequestHandlerFilterRunner(RequestHandlerFilter<TRequest, TResponse> filter, Func<TRequest, TResponse> next)
        {
            this.filter = filter;
            this.next = next;
        }

        public Func<TRequest, TResponse> GetDelegate() => Invoke;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        TResponse Invoke(TRequest request)
        {
            return filter.Invoke(request, next);
        }
    }
}
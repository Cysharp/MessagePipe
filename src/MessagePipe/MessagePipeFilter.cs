using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MessagePipe
{
    // TODO:MessageHandlerFilter, RequestHandlerFilter, etc...?

    internal interface IMessagePipeAttribute
    {
        public Type Type { get; }
        public int Order { get; }
    }

    internal interface IMessagePipeFilter
    {

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

    internal readonly struct FilterAndOrder<T>
        where T : IMessagePipeFilter
    {
        public readonly T Filter;
        public readonly int Order;

        public FilterAndOrder(T filter, int order)
        {
            Filter = filter;
            Order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class MessageHandlerFilterAttribute : Attribute, IMessagePipeAttribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        public MessageHandlerFilterAttribute(Type type)
        {
            if (!typeof(MessageHandlerFilter).IsAssignableFrom(type))
            {
                throw new ArgumentException($"{type.FullName} is not MessageHandlerFilter.");
            }
            this.Type = type;
        }
    }



    public abstract class RequestHandlerFilter
    {
        public abstract TResponse Execute<TRequest, TResponse>(TRequest request, Func<TRequest, TResponse> next);
    }

    // TODO:AsyncHandler?

    public abstract class MessageHandlerFilter : IMessagePipeFilter
    {
        public abstract void Handle<T>(T message, Action<T> next);
    }

    internal sealed class FilterAttachedMessageHandler<T> : IMessageHandler<T>
    {
        Action<T> handler;

        public FilterAttachedMessageHandler(IMessageHandler<T> body, IEnumerable<FilterAndOrder<MessageHandlerFilter>> filters)
        {
            Action<T> next = body.Handle;
            foreach (var f in filters.OrderByDescending(x => x.Order))
            {
                next = new MessageHandlerFilterRunner<T>(f.Filter, next).GetDelegate();
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
        readonly MessageHandlerFilter filter;
        readonly Action<T> next;

        public MessageHandlerFilterRunner(MessageHandlerFilter filter, Action<T> next)
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
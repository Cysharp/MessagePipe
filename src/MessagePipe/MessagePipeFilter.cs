using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MessagePipe
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class MessagePipeFilterAttribute : Attribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        public MessagePipeFilterAttribute(Type type)
        {
            this.Type = type;
        }
    }

    internal interface IAttachedFilter
    {
        internal MessagePipeFilterAttribute[] Filters { get; }
    }



    public abstract class RequestHandlerFilter
    {
        public int Order { get; set; }
        public abstract TResponse Execute<TRequest, TResponse>(TRequest request, Func<TRequest, TResponse> next);
    }

    // TODO:AsyncHandler?

    public abstract class MessageHandlerFilter
    {
        public int Order { get; set; }
        public abstract void Handle<T>(T message, Action<T> next);
    }

    internal sealed class FilterAttachedMessageHandler<T> : IMessageHandler<T>
    {
        Action<T> handler;

        public FilterAttachedMessageHandler(IMessageHandler<T> body, IEnumerable<MessageHandlerFilter> filters)
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

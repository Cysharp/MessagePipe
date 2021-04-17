using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    public interface IMessagePipeFilterAttribute
    {
        public Type Type { get; }
        public int Order { get; }
    }

    public interface IMessagePipeFilter
    {
        int Order { get; set; }
    }

    // TODO: use this????
    public abstract class RequestHandlerFilter
    {
        public abstract TResponse Execute<TRequest, TResponse>(TRequest request, Func<TRequest, TResponse> next);
    }

    // Sync filter

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class MessageHandlerFilterAttribute : Attribute, IMessagePipeFilterAttribute
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

    public abstract class MessageHandlerFilter : IMessagePipeFilter
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

    // Async filter

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class AsyncMessageHandlerFilterAttribute : Attribute, IMessagePipeFilterAttribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        public AsyncMessageHandlerFilterAttribute(Type type)
        {
            if (!typeof(AsyncMessageHandlerFilter).IsAssignableFrom(type))
            {
                throw new ArgumentException($"{type.FullName} is not AsyncMessageHandlerFilter.");
            }
            this.Type = type;
        }
    }

    public abstract class AsyncMessageHandlerFilter : IMessagePipeFilter
    {
        public int Order { get; set; }
        public abstract ValueTask HandleAsync<T>(T message, CancellationToken cancellationToken, Func<T, CancellationToken, ValueTask> next);
    }

    internal sealed class FilterAttachedAsyncMessageHandler<T> : IAsyncMessageHandler<T>
    {
        Func<T, CancellationToken, ValueTask> handler;

        public FilterAttachedAsyncMessageHandler(IAsyncMessageHandler<T> body, IEnumerable<AsyncMessageHandlerFilter> filters)
        {
            Func<T, CancellationToken, ValueTask> next = body.HandleAsync;
            foreach (var f in filters.OrderByDescending(x => x.Order))
            {
                next = new AsyncMessageHandlerFilterRunner<T>(f, next).GetDelegate();
            }

            this.handler = next;
        }

        public ValueTask HandleAsync(T message, CancellationToken cancellationToken)
        {
            return handler.Invoke(message, cancellationToken);
        }
    }

    internal sealed class AsyncMessageHandlerFilterRunner<T>
    {
        readonly AsyncMessageHandlerFilter filter;
        readonly Func<T, CancellationToken, ValueTask> next;

        public AsyncMessageHandlerFilterRunner(AsyncMessageHandlerFilter filter, Func<T, CancellationToken, ValueTask> next)
        {
            this.filter = filter;
            this.next = next;
        }

        public Func<T, CancellationToken, ValueTask> GetDelegate() => HandleAsync;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ValueTask HandleAsync(T message, CancellationToken cancellationToken)
        {
            return filter.HandleAsync(message, cancellationToken, next);
        }
    }
}
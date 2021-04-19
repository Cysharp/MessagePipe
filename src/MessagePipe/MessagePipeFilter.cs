using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    // Sync filter

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class MessageHandlerFilterAttribute : Attribute, IMessagePipeFilterAttribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        public MessageHandlerFilterAttribute(Type type)
        {
            if (!typeof(IMessageHandlerFilter).IsAssignableFrom(type))
            {
                throw new ArgumentException($"{type.FullName} is not MessageHandlerFilter.");
            }
            this.Type = type;
        }
    }

    public interface IMessageHandlerFilter : IMessagePipeFilter
    {
    }

    public abstract class MessageHandlerFilter<TMessage> : IMessageHandlerFilter
    {
        public int Order { get; set; }
        public abstract void Handle(TMessage message, Action<TMessage> next);
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

    // Req-Res Filter

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequestHandlerFilterAttribute : Attribute, IMessagePipeFilterAttribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        public RequestHandlerFilterAttribute(Type type)
        {
            if (!typeof(RequestHandlerFilter).IsAssignableFrom(type))
            {
                throw new ArgumentException($"{type.FullName} is not RequestHandlerFilter.");
            }
            this.Type = type;
        }
    }

    public abstract class RequestHandlerFilter : IMessagePipeFilter
    {
        public int Order { get; set; }
        public abstract TResponse Invoke<TRequest, TResponse>(TRequest request, Func<TRequest, TResponse> next);
    }

    internal sealed class RequestHandlerFilterRunner<TRequest, TResponse>
    {
        readonly RequestHandlerFilter filter;
        readonly Func<TRequest, TResponse> next;

        public RequestHandlerFilterRunner(RequestHandlerFilter filter, Func<TRequest, TResponse> next)
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

    // async Req-Res


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class AsyncRequestHandlerFilterAttribute : Attribute, IMessagePipeFilterAttribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        public AsyncRequestHandlerFilterAttribute(Type type)
        {
            if (!typeof(AsyncRequestHandlerFilter).IsAssignableFrom(type))
            {
                throw new ArgumentException($"{type.FullName} is not AsyncRequestHandlerFilter.");
            }
            this.Type = type;
        }
    }

    public abstract class AsyncRequestHandlerFilter : IMessagePipeFilter
    {
        public int Order { get; set; }
        public abstract ValueTask<TResponse> InvokeAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, ValueTask<TResponse>> next);
    }

    internal sealed class AsyncRequestHandlerFilterRunner<TRequest, TResponse>
    {
        readonly AsyncRequestHandlerFilter filter;
        readonly Func<TRequest, CancellationToken, ValueTask<TResponse>> next;

        public AsyncRequestHandlerFilterRunner(AsyncRequestHandlerFilter filter, Func<TRequest, CancellationToken, ValueTask<TResponse>> next)
        {
            this.filter = filter;
            this.next = next;
        }

        public Func<TRequest, CancellationToken, ValueTask<TResponse>> GetDelegate() => InvokeAsync;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken)
        {
            return filter.InvokeAsync(request, cancellationToken, next);
        }
    }
}
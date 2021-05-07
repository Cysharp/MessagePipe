using MessagePipe.Internal;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    public interface IMessagePipeFilterAttribute
    {
        Type Type { get; }
        int Order { get; }
    }

    public interface IMessagePipeFilter
    {
        int Order { get; set; }
    }

    // Sync filter

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    [Preserve]
    public class MessageHandlerFilterAttribute : Attribute, IMessagePipeFilterAttribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        [Preserve]
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

    // Async filter

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    [Preserve]
    public class AsyncMessageHandlerFilterAttribute : Attribute, IMessagePipeFilterAttribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        [Preserve]
        public AsyncMessageHandlerFilterAttribute(Type type)
        {
            if (!typeof(IAsyncMessageHandlerFilter).IsAssignableFrom(type))
            {
                throw new ArgumentException($"{type.FullName} is not AsyncMessageHandlerFilter.");
            }
            this.Type = type;
        }
    }

    public interface IAsyncMessageHandlerFilter : IMessagePipeFilter
    {
    }


    public abstract class AsyncMessageHandlerFilter<TMessage> : IAsyncMessageHandlerFilter
    {
        public int Order { get; set; }
        public abstract UniTask HandleAsync(TMessage message, CancellationToken cancellationToken, Func<TMessage, CancellationToken, UniTask> next);
    }

    // Req-Res Filter

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    [Preserve]
    public class RequestHandlerFilterAttribute : Attribute, IMessagePipeFilterAttribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        [Preserve]
        public RequestHandlerFilterAttribute(Type type)
        {
            if (!typeof(IRequestHandlerFilter).IsAssignableFrom(type))
            {
                throw new ArgumentException($"{type.FullName} is not RequestHandlerFilter.");
            }
            this.Type = type;
        }
    }

    public interface IRequestHandlerFilter : IMessagePipeFilter
    {

    }

    public abstract class RequestHandlerFilter<TRequest, TResponse> : IRequestHandlerFilter
    {
        public int Order { get; set; }
        public abstract TResponse Invoke(TRequest request, Func<TRequest, TResponse> next);
    }

    // async Req-Res

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    [Preserve]
    public class AsyncRequestHandlerFilterAttribute : Attribute, IMessagePipeFilterAttribute
    {
        public Type Type { get; }
        public int Order { get; set; }

        [Preserve]
        public AsyncRequestHandlerFilterAttribute(Type type)
        {
            if (!typeof(IAsyncRequestHandlerFilter).IsAssignableFrom(type))
            {
                throw new ArgumentException($"{type.FullName} is not AsyncRequestHandlerFilter.");
            }
            this.Type = type;
        }
    }

    public interface IAsyncRequestHandlerFilter : IMessagePipeFilter
    {

    }

    public abstract class AsyncRequestHandlerFilter<TRequest, TResponse> : IAsyncRequestHandlerFilter
    {
        public int Order { get; set; }
        public abstract UniTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, UniTask<TResponse>> next);
    }
}
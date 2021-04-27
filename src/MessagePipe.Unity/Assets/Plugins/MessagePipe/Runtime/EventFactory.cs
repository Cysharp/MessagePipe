using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    public sealed class EventFactory
    {
        readonly MessagePipeOptions options;
        readonly MessagePipeDiagnosticsInfo diagnosticsInfo;
        readonly FilterAttachedMessageHandlerFactory handlerFactory;
        readonly FilterAttachedAsyncMessageHandlerFactory asyncHandlerFactory;

        public EventFactory(
            MessagePipeOptions options,
            MessagePipeDiagnosticsInfo diagnosticsInfo,
            FilterAttachedMessageHandlerFactory handlerFactory,
            FilterAttachedAsyncMessageHandlerFactory asyncHandlerFactory)
        {
            this.options = options;
            this.diagnosticsInfo = diagnosticsInfo;
            this.handlerFactory = handlerFactory;
            this.asyncHandlerFactory = asyncHandlerFactory;
        }

        public (IDisposablePublisher<T>, ISubscriber<T>) CreateEvent<T>()
        {
            var core = new MessageBrokerCore<T>(diagnosticsInfo, options);
            var publisher = new DisposablePublisher<T>(core);
            var subscriber = new MessageBroker<T>(core, handlerFactory);
            return (publisher, subscriber);
        }

        public (IDisposableAsyncPublisher<T>, IAsyncSubscriber<T>) CreateAsyncEvent<T>()
        {
            var core = new AsyncMessageBrokerCore<T>(diagnosticsInfo, options);
            var publisher = new DisposableAsyncPublisher<T>(core);
            var subscriber = new AsyncMessageBroker<T>(core, asyncHandlerFactory);
            return (publisher, subscriber);
        }
    }

    public interface IDisposablePublisher<TMessage> : IPublisher<TMessage>, IDisposable
    {
    }

    internal class DisposablePublisher<TMessage> : IDisposablePublisher<TMessage>
    {
        readonly MessageBrokerCore<TMessage> core;

        public DisposablePublisher(MessageBrokerCore<TMessage> core)
        {
            this.core = core;
        }

        public void Publish(TMessage message)
        {
            core.Publish(message);
        }

        public void Dispose()
        {
            core.Dispose();
        }
    }

    public interface IDisposableAsyncPublisher<TMessage> : IAsyncPublisher<TMessage>, IDisposable
    {
    }

    internal sealed class DisposableAsyncPublisher<TMessage> : IDisposableAsyncPublisher<TMessage>
    {
        readonly AsyncMessageBrokerCore<TMessage> core;

        public DisposableAsyncPublisher(AsyncMessageBrokerCore<TMessage> core)
        {
            this.core = core;
        }

        public void Publish(TMessage message, CancellationToken cancellationToken)
        {
            core.Publish(message, cancellationToken);
        }

        public UniTask PublishAsync(TMessage message, CancellationToken cancellationToken)
        {
            return core.PublishAsync(message, cancellationToken);
        }

        public UniTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken)
        {
            return core.PublishAsync(message, publishStrategy, cancellationToken);
        }

        public void Dispose()
        {
            core.Dispose();
        }
    }
}
using MessagePipe.Internal;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    [Preserve]
    public sealed class EventFactory
    {
        readonly MessagePipeOptions options;
        readonly MessagePipeDiagnosticsInfo diagnosticsInfo;
        readonly FilterAttachedMessageHandlerFactory handlerFactory;
        readonly FilterAttachedAsyncMessageHandlerFactory asyncHandlerFactory;

        [Preserve]
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

        public (IDisposableBufferedPublisher<T>, IBufferedSubscriber<T>) CreateBufferedEvent<T>(T initialValue)
        {
            var innerCore = new MessageBrokerCore<T>(diagnosticsInfo, options);
            var core = new BufferedMessageBrokerCore<T>(innerCore);
            var broker = new BufferedMessageBroker<T>(core, handlerFactory);
            var publisher = new DisposableBufferedPublisher<T>(broker, innerCore);
            var subscriber = broker;
            publisher.Publish(initialValue);
            return (publisher, subscriber);
        }

        public (IDisposableBufferedAsyncPublisher<T>, IBufferedAsyncSubscriber<T>) CreateBufferedAsyncEvent<T>(T initialValue)
        {
            var innerCore = new AsyncMessageBrokerCore<T>(diagnosticsInfo, options);
            var core = new BufferedAsyncMessageBrokerCore<T>(innerCore);
            var broker = new BufferedAsyncMessageBroker<T>(core, asyncHandlerFactory);
            var publisher = new DisposableBufferedAsyncPublisher<T>(broker, innerCore);
            var subscriber = broker;
            publisher.Publish(initialValue, CancellationToken.None); // set initial value is completely sync.
            return (publisher, subscriber);
        }
    }

    public interface IDisposablePublisher<TMessage> : IPublisher<TMessage>, IDisposable
    {
    }

    public interface IDisposableBufferedPublisher<TMessage> : IBufferedPublisher<TMessage>, IDisposable
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

    internal class DisposableBufferedPublisher<TMessage> : IDisposableBufferedPublisher<TMessage>
    {
        readonly BufferedMessageBroker<TMessage> broker;
        readonly IDisposable disposable;

        public DisposableBufferedPublisher(BufferedMessageBroker<TMessage> broker, IDisposable disposable)
        {
            this.broker = broker;
            this.disposable = disposable;
        }

        public void Publish(TMessage message)
        {
            broker.Publish(message);
        }

        public void Dispose()
        {
            disposable.Dispose();
        }
    }

    public interface IDisposableAsyncPublisher<TMessage> : IAsyncPublisher<TMessage>, IDisposable
    {
    }

    public interface IDisposableBufferedAsyncPublisher<TMessage> : IBufferedAsyncPublisher<TMessage>, IDisposable
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

    internal sealed class DisposableBufferedAsyncPublisher<TMessage> : IDisposableBufferedAsyncPublisher<TMessage>
    {
        readonly BufferedAsyncMessageBroker<TMessage> broker;
        readonly IDisposable disposable;

        public DisposableBufferedAsyncPublisher(BufferedAsyncMessageBroker<TMessage> broker, IDisposable disposable)
        {
            this.broker = broker;
            this.disposable = disposable;
        }

        public void Publish(TMessage message, CancellationToken cancellationToken)
        {
            broker.Publish(message, cancellationToken);
        }

        public UniTask PublishAsync(TMessage message, CancellationToken cancellationToken)
        {
            return broker.PublishAsync(message, cancellationToken);
        }

        public UniTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken)
        {
            return broker.PublishAsync(message, publishStrategy, cancellationToken);
        }

        public void Dispose()
        {
            disposable.Dispose();
        }
    }
}
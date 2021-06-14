using MessagePipe.Interprocess.Internal;
using MessagePipe.Interprocess.Workers;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe.Interprocess
{
    [Preserve]
    public sealed class NamedPipeDistributedPublisher<TKey, TMessage> : IDistributedPublisher<TKey, TMessage>
    {
        readonly NamedPipeWorker worker;

        [Preserve]
        public NamedPipeDistributedPublisher(NamedPipeWorker worker)
        {
            this.worker = worker;
        }

        public UniTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default)
        {
            worker.Publish(key, message);
            return default;
        }
    }

    [Preserve]
    public sealed class NamedPipeDistributedSubscriber<TKey, TMessage> : IDistributedSubscriber<TKey, TMessage>
    {
        // Pubsished from worker.
        readonly MessagePipeInterprocessNamedPipeOptions options;
        readonly IAsyncSubscriber<IInterprocessKey, IInterprocessValue> subscriberCore;
        readonly FilterAttachedMessageHandlerFactory syncHandlerFactory;
        readonly FilterAttachedAsyncMessageHandlerFactory asyncHandlerFactory;

        [Preserve]
        public NamedPipeDistributedSubscriber(NamedPipeWorker worker, MessagePipeInterprocessNamedPipeOptions options, IAsyncSubscriber<IInterprocessKey, IInterprocessValue> subscriberCore, FilterAttachedMessageHandlerFactory syncHandlerFactory, FilterAttachedAsyncMessageHandlerFactory asyncHandlerFactory)
        {
            this.options = options;
            this.subscriberCore = subscriberCore;
            this.syncHandlerFactory = syncHandlerFactory;
            this.asyncHandlerFactory = asyncHandlerFactory;

            worker.StartReceiver();
        }

        public UniTask<IUniTaskAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(key, handler, Array.Empty<MessageHandlerFilter<TMessage>>(), cancellationToken);
        }

        public UniTask<IUniTaskAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, MessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            handler = syncHandlerFactory.CreateMessageHandler(handler, filters);
            var transform = new TransformSyncMessageHandler<TMessage>(handler, options.MessagePackSerializerOptions);
            return SubscribeCore(key, transform);
        }

        public UniTask<IUniTaskAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(key, handler, Array.Empty<AsyncMessageHandlerFilter<TMessage>>(), cancellationToken);
        }

        public UniTask<IUniTaskAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            handler = asyncHandlerFactory.CreateAsyncMessageHandler(handler, filters);
            var transform = new TransformAsyncMessageHandler<TMessage>(handler, options.MessagePackSerializerOptions);
            return SubscribeCore(key, transform);
        }

        UniTask<IUniTaskAsyncDisposable> SubscribeCore(TKey key, IAsyncMessageHandler<IInterprocessValue> handler)
        {
            var byteKey = MessageBuilder.CreateKey(key, options.MessagePackSerializerOptions);
            var d = subscriberCore.Subscribe(byteKey, handler);
            return new UniTask<IUniTaskAsyncDisposable>(new AsyncDisposableBridge(d));
        }
    }


}

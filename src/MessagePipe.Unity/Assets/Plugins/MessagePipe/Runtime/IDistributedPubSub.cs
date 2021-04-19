#if !UNITY_2018_3_OR_NEWER

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    public interface IDistributedPublisher<TKey, TMessage>
    {
        UniTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default);
    }

    public interface IDistributedSubscriber<TKey, TMessage>
    {
        public UniTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, CancellationToken cancellationToken = default);
        public UniTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, MessageHandlerFilter[] filters, CancellationToken cancellationToken = default);
        public UniTask<IAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, CancellationToken cancellationToken = default);
        public UniTask<IAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter[] filters, CancellationToken cancellationToken = default);
    }
}

#endif
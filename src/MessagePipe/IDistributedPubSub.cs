using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    public interface IDistributedPublisher<TKey, TMessage>
    {
        ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default);
    }

    public interface IDistributedSubscriber<TKey, TMessage>
    {
        public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, CancellationToken cancellationToken = default);
        public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, MessageHandlerFilter[] filters, CancellationToken cancellationToken = default);
    }
}

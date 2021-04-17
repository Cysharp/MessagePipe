using System;
using System.Threading.Tasks;

namespace MessagePipe
{
    public interface IDistributedPublisher<TKey, TMessage>
    {
        ValueTask PublishAsync(TKey key, TMessage message);
    }

    public interface IDistributedSubscriber<TKey, TMessage>
    {
        public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler);
    }
}

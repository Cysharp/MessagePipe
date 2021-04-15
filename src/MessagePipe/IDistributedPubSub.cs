using System;
using System.Threading.Tasks;

namespace MessagePipe
{
    public interface IDistributedPublisher<TKey, TMessage>
    {
        Task PublishAsync(TKey key, TMessage message);
    }

    public interface IDistributedSubscriber<TKey, TMessage>
    {
        public Task<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler);
    }
}

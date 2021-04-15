using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace MessagePipe.Redis
{
    public interface IRedisSerializer
    {
        byte[] Serialize<T>(T value);
        T Deserialize<T>(byte[] value);
    }

    public sealed class RedisPublisher<TKey, TMessage> : IDistributedPublisher<TKey, TMessage>
    {
        IRedisSerializer serializer;
        ISubscriber subscriber;

        public RedisPublisher(IConnectionMultiplexer connection, IRedisSerializer serializer)
        {
            this.subscriber = connection.GetSubscriber();
            this.serializer = serializer;
        }

        public async Task PublishAsync(TKey key, TMessage message)
        {
            var channel = CreateChannel(key);
            var value = serializer.Serialize(message);
            await subscriber.PublishAsync(channel, value);
        }

        RedisChannel CreateChannel(TKey key)
        {
            return (key is string s)
                ? new RedisChannel(s, RedisChannel.PatternMode.Auto) // use Auto.
                : new RedisChannel(serializer.Serialize(key), RedisChannel.PatternMode.Literal);
        }
    }

    public sealed class RedisSubscriber<TKey, TMessage> : IDistributedSubscriber<TKey, TMessage>
    {
        IRedisSerializer serializer;
        ISubscriber subscriber;

        public RedisSubscriber(IConnectionMultiplexer connection, IRedisSerializer serializer)
        {
            this.subscriber = connection.GetSubscriber();
            this.serializer = serializer;
        }

        public async Task<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler)
        {
            var channel = CreateChannel(key);

            var mq = await subscriber.SubscribeAsync(channel).ConfigureAwait(false);
            mq.OnMessage(message =>
            {
                var v = serializer.Deserialize<TMessage>((byte[])message.Message);
                handler.Handle(v);
            });

            return new Subscription(mq);
        }

        RedisChannel CreateChannel(TKey key)
        {
            return (key is string s)
                ? new RedisChannel(s, RedisChannel.PatternMode.Auto) // use Auto.
                : new RedisChannel(serializer.Serialize(key), RedisChannel.PatternMode.Literal);
        }

        sealed class Subscription : IAsyncDisposable
        {
            readonly ChannelMessageQueue mq;

            public Subscription(ChannelMessageQueue mq)
            {
                this.mq = mq;
            }

            public async ValueTask DisposeAsync()
            {
                await mq.UnsubscribeAsync();
            }
        }
    }
}
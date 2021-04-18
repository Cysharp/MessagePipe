using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe.Redis
{
    public interface IRedisSerializer
    {
        byte[] Serialize<T>(T value);
        T Deserialize<T>(byte[] value);
    }

    public interface IConnectionMultiplexerFactory
    {
        public IConnectionMultiplexer GetConnectionMultiplexer();
    }

    public sealed class RedisPublisher<TKey, TMessage> : IDistributedPublisher<TKey, TMessage>
    {
        readonly IRedisSerializer serializer;
        readonly IConnectionMultiplexerFactory connectionFactory;

        public RedisPublisher(IConnectionMultiplexerFactory connectionFactory, IRedisSerializer serializer)
        {
            this.connectionFactory = connectionFactory;
            this.serializer = serializer;
        }

        public async ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken)
        {
            var channel = CreateChannel(key);
            var value = serializer.Serialize(message);

            // Redis.PublishAsync has no cancellationToken overload.
            await connectionFactory.GetConnectionMultiplexer().GetSubscriber().PublishAsync(channel, value).ConfigureAwait(false);
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
        readonly IConnectionMultiplexerFactory connectionFactory;

        public RedisSubscriber(IConnectionMultiplexerFactory connectionFactory, IRedisSerializer serializer)
        {
            this.connectionFactory = connectionFactory;
            this.serializer = serializer;
        }

        public async ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, CancellationToken cancellationToken)
        {
            var channel = CreateChannel(key);

            var mq = await connectionFactory.GetConnectionMultiplexer().GetSubscriber().SubscribeAsync(channel).ConfigureAwait(false);
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
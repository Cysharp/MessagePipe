using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe.Redis
{
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
            await connectionFactory.GetConnectionMultiplexer(key).GetSubscriber().PublishAsync(channel, value).ConfigureAwait(false);
        }

        RedisChannel CreateChannel(TKey key)
        {
            switch (key)
            {
                case string s:
                    return new RedisChannel(s, RedisChannel.PatternMode.Auto); // use Auto.
                case byte[] v:
                    return new RedisChannel(v, RedisChannel.PatternMode.Auto);
                default:
                    return new RedisChannel(serializer.Serialize(key), RedisChannel.PatternMode.Literal);
            }
        }
    }

    public sealed class RedisSubscriber<TKey, TMessage> : IDistributedSubscriber<TKey, TMessage>
    {
        IRedisSerializer serializer;
        readonly IConnectionMultiplexerFactory connectionFactory;
        readonly FilterAttachedMessageHandlerFactory messageHandlerFactory;
        readonly FilterAttachedAsyncMessageHandlerFactory asyncMessageHandlerFactory;

        public RedisSubscriber(IConnectionMultiplexerFactory connectionFactory, IRedisSerializer serializer, FilterAttachedMessageHandlerFactory messageHandlerFactory, FilterAttachedAsyncMessageHandlerFactory asyncMessageHandlerFactory)
        {
            this.connectionFactory = connectionFactory;
            this.serializer = serializer;
            this.messageHandlerFactory = messageHandlerFactory;
            this.asyncMessageHandlerFactory = asyncMessageHandlerFactory;
        }

        public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, CancellationToken cancellationToken)
        {
            return SubscribeAsync(key, handler, Array.Empty<MessageHandlerFilter>(), cancellationToken);
        }

        public async ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, MessageHandlerFilter[] filters, CancellationToken cancellationToken)
        {
            handler = messageHandlerFactory.CreateMessageHandler(handler, filters); // with filter

            var channel = CreateChannel(key);

            var mq = await connectionFactory.GetConnectionMultiplexer(key).GetSubscriber().SubscribeAsync(channel).ConfigureAwait(false);
            mq.OnMessage(message =>
            {
                var v = serializer.Deserialize<TMessage>((byte[])message.Message);
                handler.Handle(v);
            });

            return new Subscription(mq);
        }

        public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, CancellationToken cancellationToken)
        {
            return SubscribeAsync(key, handler, Array.Empty<AsyncMessageHandlerFilter>(), cancellationToken);
        }

        public async ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter[] filters, CancellationToken cancellationToken)
        {
            handler = asyncMessageHandlerFactory.CreateAsyncMessageHandler(handler, filters); // with filter

            var channel = CreateChannel(key);

            var mq = await connectionFactory.GetConnectionMultiplexer(key).GetSubscriber().SubscribeAsync(channel).ConfigureAwait(false);
            mq.OnMessage(async message =>
            {
                var v = serializer.Deserialize<TMessage>((byte[])message.Message);
                await handler.HandleAsync(v, CancellationToken.None).ConfigureAwait(false);
            });

            return new Subscription(mq);
        }

        RedisChannel CreateChannel(TKey key)
        {
            switch (key)
            {
                case string s:
                    return new RedisChannel(s, RedisChannel.PatternMode.Auto); // use Auto.
                case byte[] v:
                    return new RedisChannel(v, RedisChannel.PatternMode.Auto);
                default:
                    return new RedisChannel(serializer.Serialize(key), RedisChannel.PatternMode.Literal);
            }
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
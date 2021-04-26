using StackExchange.Redis;

namespace MessagePipe.Redis
{
    public interface IRedisSerializer
    {
        byte[] Serialize<T>(T value);
        T Deserialize<T>(byte[] value);
    }

    public interface IConnectionMultiplexerFactory
    {
        public IConnectionMultiplexer GetConnectionMultiplexer<TKey>(TKey key);
    }

    public sealed class MessagePipeRedisOptions
    {
        public IConnectionMultiplexerFactory ConnectionMultiplexerFactory { get; }
        public IRedisSerializer RedisSerializer { get; set; }

        public MessagePipeRedisOptions(IConnectionMultiplexerFactory connectionMultiplexerFactory)
        {
            this.RedisSerializer = new MessagePackRedisSerializer();
            this.ConnectionMultiplexerFactory = connectionMultiplexerFactory;
        }
    }
}
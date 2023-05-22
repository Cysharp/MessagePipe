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
        public IRedisSerializer RedisSerializer { get; set; }

        public MessagePipeRedisOptions()
        {
            this.RedisSerializer = new MessagePackRedisSerializer();
        }
    }
}
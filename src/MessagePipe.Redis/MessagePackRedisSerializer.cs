using MessagePack;
using MessagePack.Resolvers;

namespace MessagePipe.Redis
{
    public sealed class MessagePackRedisSerializer : IRedisSerializer
    {
        readonly MessagePackSerializerOptions options;

        public MessagePackRedisSerializer()
        {
            options = ContractlessStandardResolver.Options;
        }

        public MessagePackRedisSerializer(MessagePackSerializerOptions options)
        {
            this.options = options;
        }

        public byte[] Serialize<T>(T value)
        {
            if (value is byte[] xs) return xs;
            return MessagePackSerializer.Serialize<T>(value, options);
        }

        public T Deserialize<T>(byte[] value)
        {
            return MessagePackSerializer.Deserialize<T>(value);
        }
    }
}
using StackExchange.Redis;

namespace MessagePipe.Redis
{
    internal sealed class SingleConnectionMultiplexerFactory : IConnectionMultiplexerFactory
    {
        readonly IConnectionMultiplexer connectionMultiplexer;

        public SingleConnectionMultiplexerFactory(IConnectionMultiplexer connectionMultiplexer)
        {
            this.connectionMultiplexer = connectionMultiplexer;
        }

        public IConnectionMultiplexer GetConnectionMultiplexer<TKey>(TKey key)
        {
            return connectionMultiplexer;
        }
    }
}
using AlterNats;

namespace MessagePipe.Nats;

public sealed class NatsPublisher<TKey, TMessage> : IDistributedPublisher<TKey, TMessage>
{
    readonly NatsConnectionFactory connectionFactory;

    public NatsPublisher(
        NatsConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = new CancellationToken())
    {
        var natsKey = GetNatsKey(key);

        if (natsKey == null) throw new ArgumentNullException(nameof(key));

        var connection = await connectionFactory.GetConnectionAsync();
        await connection.PublishAsync(natsKey.Value, message);
    }

    NatsKey? GetNatsKey(TKey key)
    {
        switch (key)
        {
            case NatsKey natsKey:
                return natsKey;
            case string s:
                return new NatsKey(s);
            default:
                var k = key?.ToString();
                return k != null ? new NatsKey(k) : null;
        }
    }
}
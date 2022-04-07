namespace MessagePipe.Nats;

public sealed class MessagePipeNatsOptions
{
    public NatsConnectionFactory NatsConnectionFactory { get; }

    public MessagePipeNatsOptions(NatsConnectionFactory connectionFactory)
    {
        NatsConnectionFactory = connectionFactory;
    }
}
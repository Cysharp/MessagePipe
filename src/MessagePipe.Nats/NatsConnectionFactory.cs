using AlterNats;

namespace MessagePipe.Nats;

public class NatsConnectionFactory
{
    readonly NatsOptions options;
    NatsConnection? connection;

    public NatsConnectionFactory() : this(NatsOptions.Default) { }

    public NatsConnectionFactory(NatsOptions options)
    {
        this.options = options;
    }

    public async ValueTask<NatsConnection> GetConnectionAsync()
    {
        connection ??= new NatsConnection(options);

        if (connection.ConnectionState == NatsConnectionState.Closed)
        {
            await connection.ConnectAsync();
        }

        return connection;
    }
}
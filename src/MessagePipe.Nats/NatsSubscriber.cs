using AlterNats;

namespace MessagePipe.Nats;

public sealed class NatsSubscriber<TKey, TMessage> : IDistributedSubscriber<TKey, TMessage>
{
    readonly NatsConnectionFactory connectionFactory;
    readonly FilterAttachedMessageHandlerFactory messageHandlerFactory;
    readonly FilterAttachedAsyncMessageHandlerFactory asyncMessageHandlerFactory;

    public NatsSubscriber(
        NatsConnectionFactory connectionFactory,
        FilterAttachedMessageHandlerFactory messageHandlerFactory,
        FilterAttachedAsyncMessageHandlerFactory asyncMessageHandlerFactory)
    {
        this.connectionFactory = connectionFactory;
        this.messageHandlerFactory = messageHandlerFactory;
        this.asyncMessageHandlerFactory = asyncMessageHandlerFactory;
    }

    public ValueTask<IAsyncDisposable> SubscribeAsync(
        TKey key,
        IMessageHandler<TMessage> handler,
        CancellationToken cancellationToken = new())
    {
        return SubscribeAsync(key, handler, Array.Empty<MessageHandlerFilter<TMessage>>(), cancellationToken);
    }

    public async ValueTask<IAsyncDisposable> SubscribeAsync(
        TKey key,
        IMessageHandler<TMessage> handler,
        MessageHandlerFilter<TMessage>[] filters,
        CancellationToken cancellationToken = new())
    {
        var subject = GetSubjectString(key);

        if (subject == null) throw new ArgumentException(nameof(key));

        handler = messageHandlerFactory.CreateMessageHandler(handler, filters); // with filter

        var connection = await connectionFactory.GetConnectionAsync();

        var s = await connection.SubscribeAsync<TMessage>(subject, data =>
        {
            handler.Handle(data);
        });

        return new Subscription(s);
    }

    public ValueTask<IAsyncDisposable> SubscribeAsync(
        TKey key,
        IAsyncMessageHandler<TMessage> handler,
        CancellationToken cancellationToken = new())
    {
        return SubscribeAsync(key, handler, Array.Empty<AsyncMessageHandlerFilter<TMessage>>(), cancellationToken);
    }

    public async ValueTask<IAsyncDisposable> SubscribeAsync(
        TKey key,
        IAsyncMessageHandler<TMessage> handler,
        AsyncMessageHandlerFilter<TMessage>[] filters,
        CancellationToken cancellationToken = new())
    {
        var subject = GetSubjectString(key);

        if (subject == null) throw new ArgumentException(nameof(key));

        handler = asyncMessageHandlerFactory.CreateAsyncMessageHandler(handler, filters); // with filter

        var connection = await connectionFactory.GetConnectionAsync();

        var s = await connection.SubscribeAsync<TMessage>(subject, async data =>
        {
            await handler.HandleAsync(data, CancellationToken.None).ConfigureAwait(false);
        });

        return new Subscription(s);
    }

    sealed class Subscription : IAsyncDisposable
    {
        readonly IDisposable disposable;

        public Subscription(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public ValueTask DisposeAsync()
        {
            disposable.Dispose();

            return ValueTask.CompletedTask;
        }
    }

    string? GetSubjectString(TKey key)
    {
        switch (key)
        {
            case NatsKey natsKey:
                return natsKey.Key;
            case string s:
                return s;
            default:
                return key?.ToString();
        }
    }
}
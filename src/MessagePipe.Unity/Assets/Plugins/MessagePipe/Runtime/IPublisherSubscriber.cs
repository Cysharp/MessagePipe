using MessagePipe.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    // handler

    public interface IMessageHandler<T>
    {
        void Handle(T message);
    }

    public interface IAsyncMessageHandler<T>
    {
        ValueTask HandleAsync(T message, CancellationToken cancellationToken);
    }

    // Keyed

    public interface IPublisher<TKey, TMessage>
        where TKey : notnull
    {
        void Publish(TKey key, TMessage message);
    }

    public interface ISubscriber<TKey, TMessage>
        where TKey : notnull
    {
        public IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler, params MessageHandlerFilter[] filters);
    }

    public interface IAsyncPublisher<TKey, TMessage>
        where TKey : notnull
    {
        void Publish(TKey key, TMessage message, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask PublishAsync(TKey key, TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IAsyncSubscriber<TKey, TMessage>
        where TKey : notnull
    {
        public IDisposable Subscribe(TKey key, IAsyncMessageHandler<TMessage> asyncHandler, params AsyncMessageHandlerFilter[] filters);
    }

    // Keyless

    public interface IPublisher<TMessage>
    {
        void Publish(TMessage message);
    }

    public interface ISubscriber<TMessage>
    {
        public IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter[] filters);
    }

    public interface IAsyncPublisher<TMessage>
    {
        void Publish(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IAsyncSubscriber<TMessage>
    {
        public IDisposable Subscribe(IAsyncMessageHandler<TMessage> asyncHandler, params AsyncMessageHandlerFilter[] filters);
    }
}
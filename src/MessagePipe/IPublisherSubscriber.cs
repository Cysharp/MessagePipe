﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    // handler

    public interface IMessageHandler<TMessage>
    {
        void Handle(TMessage message);
    }

    public interface IAsyncMessageHandler<TMessage>
    {
        ValueTask HandleAsync(TMessage message, CancellationToken cancellationToken);
    }

    // Keyless

    public interface IPublisher<TMessage>
    {
        void Publish(TMessage message);
    }

    public interface ISubscriber<TMessage>
    {
        IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters);
    }

    public interface IAsyncPublisher<TMessage>
    {
        void Publish(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IAsyncSubscriber<TMessage>
    {
        IDisposable Subscribe(IAsyncMessageHandler<TMessage> asyncHandler, params AsyncMessageHandlerFilter<TMessage>[] filters);
    }

    public interface ISingletonPublisher<TMessage> : IPublisher<TMessage> { }
    public interface ISingletonSubscriber<TMessage> : ISubscriber<TMessage> { }
    public interface IScopedPublisher<TMessage> : IPublisher<TMessage> { }
    public interface IScopedSubscriber<TMessage> : ISubscriber<TMessage> { }
    public interface ISingletonAsyncPublisher<TMessage> : IAsyncPublisher<TMessage> { }
    public interface ISingletonAsyncSubscriber<TMessage> : IAsyncSubscriber<TMessage> { }
    public interface IScopedAsyncPublisher<TMessage> : IAsyncPublisher<TMessage> { }
    public interface IScopedAsyncSubscriber<TMessage> : IAsyncSubscriber<TMessage> { }

    // Keyed

    public interface IPublisher<TKey, TMessage>
        where TKey : notnull
    {
        void Publish(TKey key, TMessage message);
    }

    public interface ISubscriber<TKey, TMessage>
        where TKey : notnull
    {
        IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters);
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
        IDisposable Subscribe(TKey key, IAsyncMessageHandler<TMessage> asyncHandler, params AsyncMessageHandlerFilter<TMessage>[] filters);
    }

    public interface ISingletonPublisher<TKey, TMessage> : IPublisher<TKey, TMessage> where TKey : notnull { }
    public interface ISingletonSubscriber<TKey, TMessage> : ISubscriber<TKey, TMessage> where TKey : notnull { }
    public interface IScopedPublisher<TKey, TMessage> : IPublisher<TKey, TMessage> where TKey : notnull { }
    public interface IScopedSubscriber<TKey, TMessage> : ISubscriber<TKey, TMessage> where TKey : notnull { }
    public interface ISingletonAsyncPublisher<TKey, TMessage> : IAsyncPublisher<TKey, TMessage> where TKey : notnull { }
    public interface ISingletonAsyncSubscriber<TKey, TMessage> : IAsyncSubscriber<TKey, TMessage> where TKey : notnull { }
    public interface IScopedAsyncPublisher<TKey, TMessage> : IAsyncPublisher<TKey, TMessage> where TKey : notnull { }
    public interface IScopedAsyncSubscriber<TKey, TMessage> : IAsyncSubscriber<TKey, TMessage> where TKey : notnull { }

    // buffered keyless

    public interface IBufferedPublisher<TMessage>
    {
        void Publish(TMessage message);
    }

    public interface IBufferedSubscriber<TMessage>
    {
        IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters);
    }

    public interface IBufferedAsyncPublisher<TMessage>
    {
        void Publish(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
        ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IBufferedAsyncSubscriber<TMessage>
    {
        ValueTask<IDisposable> SubscribeAsync(IAsyncMessageHandler<TMessage> handler, CancellationToken cancellationToken = default);
        ValueTask<IDisposable> SubscribeAsync(IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default);
    }

    // NOTE: buffered Keyed is undefined
    // because difficult to avoid (unused)key and keep latest value memory leak.
}
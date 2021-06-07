using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#if !UNITY_2018_3_OR_NEWER
using System.Threading.Channels;
#endif

namespace MessagePipe
{
    public static partial class SubscriberExtensions
    {
        public static IAsyncEnumerable<TMessage> AsAsyncEnumerable<TMessage>(this IAsyncSubscriber<TMessage> subscriber, params AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            return new AsyncEnumerableAsyncSubscriber<TMessage>(subscriber, filters);
        }

        public static IAsyncEnumerable<TMessage> AsAsyncEnumerable<TMessage>(this IBufferedAsyncSubscriber<TMessage> subscriber, params AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            return new BufferedAsyncEnumerableAsyncSubscriber<TMessage>(subscriber, filters);
        }

        public static IAsyncEnumerable<TMessage> AsAsyncEnumerable<TKey, TMessage>(this IAsyncSubscriber<TKey, TMessage> subscriber, TKey key, params AsyncMessageHandlerFilter<TMessage>[] filters)
            where TKey : notnull
        {
            return new AsyncEnumerableAsyncSubscriber<TKey, TMessage>(key, subscriber, filters);
        }
    }

    internal class AsyncEnumerableAsyncSubscriber<TMessage> : IAsyncEnumerable<TMessage>
    {
        readonly IAsyncSubscriber<TMessage> subscriber;
        readonly AsyncMessageHandlerFilter<TMessage>[] filters;

        public AsyncEnumerableAsyncSubscriber(IAsyncSubscriber<TMessage> subscriber, AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            this.subscriber = subscriber;
            this.filters = filters;
        }

        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var disposable = DisposableBag.CreateSingleAssignment();
            var e = new AsyncMessageHandlerEnumerator<TMessage>(disposable, cancellationToken);
            disposable.Disposable = subscriber.Subscribe(e, filters);
            return e;
        }
    }

    internal class BufferedAsyncEnumerableAsyncSubscriber<TMessage> : IAsyncEnumerable<TMessage>
    {
        readonly IBufferedAsyncSubscriber<TMessage> subscriber;
        readonly AsyncMessageHandlerFilter<TMessage>[] filters;

        public BufferedAsyncEnumerableAsyncSubscriber(IBufferedAsyncSubscriber<TMessage> subscriber, AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            this.subscriber = subscriber;
            this.filters = filters;
        }

        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var disposable = DisposableBag.CreateSingleAssignment();
            var e = new AsyncMessageHandlerEnumerator<TMessage>(disposable, cancellationToken);
            var task = subscriber.SubscribeAsync(e, filters);
            SetDisposableAsync(task, disposable);
            return e;
        }

        async void SetDisposableAsync(ValueTask<IDisposable> task, SingleAssignmentDisposable d)
        {
            d.Disposable = await task;
        }
    }

    internal class AsyncEnumerableAsyncSubscriber<TKey, TMessage> : IAsyncEnumerable<TMessage>
        where TKey : notnull
    {
        readonly TKey key;
        readonly IAsyncSubscriber<TKey, TMessage> subscriber;
        readonly AsyncMessageHandlerFilter<TMessage>[] filters;

        public AsyncEnumerableAsyncSubscriber(TKey key, IAsyncSubscriber<TKey, TMessage> subscriber, AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            this.key = key;
            this.subscriber = subscriber;
            this.filters = filters;
        }

        public IAsyncEnumerator<TMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var disposable = DisposableBag.CreateSingleAssignment();
            var e = new AsyncMessageHandlerEnumerator<TMessage>(disposable, cancellationToken);
            disposable.Disposable = subscriber.Subscribe(key, e, filters);
            return e;
        }
    }

    internal class AsyncMessageHandlerEnumerator<TMessage> : IAsyncEnumerator<TMessage>, IAsyncMessageHandler<TMessage>
    {
        Channel<TMessage> channel;
        CancellationToken cancellationToken;
        SingleAssignmentDisposable singleAssignmentDisposable;

        public AsyncMessageHandlerEnumerator(SingleAssignmentDisposable singleAssignmentDisposable, CancellationToken cancellationToken)
        {
            this.singleAssignmentDisposable = singleAssignmentDisposable;
            this.cancellationToken = cancellationToken;
#if !UNITY_2018_3_OR_NEWER
            this.channel = Channel.CreateUnbounded<TMessage>(new UnboundedChannelOptions()
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = true
            });
#else
                this.channel = Channel.CreateSingleConsumerUnbounded<TMessage>();
#endif
        }

        TMessage IAsyncEnumerator<TMessage>.Current
        {
            get
            {
                if (channel.Reader.TryRead(out var msg))
                {
                    return msg;
                }
                throw new InvalidOperationException("Message is not buffered in Channel.");
            }
        }

        ValueTask<bool> IAsyncEnumerator<TMessage>.MoveNextAsync()
        {
            return channel.Reader.WaitToReadAsync(cancellationToken);
        }

        ValueTask IAsyncMessageHandler<TMessage>.HandleAsync(TMessage message, CancellationToken cancellationToken)
        {
            channel.Writer.TryWrite(message);
            return default;
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            singleAssignmentDisposable.Dispose(); // unsubscribe message.
            return default;
        }
    }
}
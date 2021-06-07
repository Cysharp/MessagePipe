using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MessagePipe
{
    public static partial class SubscriberExtensions
    {
        public static IAsyncEnumerable<TMessage> AsAsyncEnumerable<TMessage>(this IAsyncSubscriber<TMessage> subscriber, params AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            return new AsyncEnumerableAsyncSubscriber<TMessage>(subscriber, filters);
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
            var e = new Enumerator(disposable, cancellationToken);
            disposable.Disposable = subscriber.Subscribe(e, filters);
            return e;
        }

        class Enumerator : IAsyncEnumerator<TMessage>, IAsyncMessageHandler<TMessage>
        {
            Channel<TMessage> channel;
            CancellationToken cancellationToken;
            SingleAssignmentDisposable singleAssignmentDisposable;

            public Enumerator(SingleAssignmentDisposable singleAssignmentDisposable, CancellationToken cancellationToken)
            {
                this.singleAssignmentDisposable = singleAssignmentDisposable;
                this.cancellationToken = cancellationToken;
                this.channel = Channel.CreateUnbounded<TMessage>(new UnboundedChannelOptions()
                {
                    SingleWriter = true,
                    SingleReader = true,
                    AllowSynchronousContinuations = true
                });
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
                return channel.Writer.WriteAsync(message, cancellationToken);
            }

            ValueTask IAsyncDisposable.DisposeAsync()
            {
                singleAssignmentDisposable.Dispose(); // unsubscribe message.
                return default;
            }
        }
    }
}
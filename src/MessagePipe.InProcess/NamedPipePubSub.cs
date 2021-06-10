using MessagePack;
using System;
using System.Buffers;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MessagePipe.InProcess
{
    public class NamedPipePublisher<TKey, TMessage> : IDistributedPublisher<TKey, TMessage>
    {
        // var pipeClient = new NamedPipeClientStream(".", "hogepipe", PipeDirection.InOut, PipeOptions.Asynchronous);
        readonly MessagePipeInProcessOptions options;

        public NamedPipePublisher(MessagePipeInProcessOptions options)
        {
            this.options = options;
        }

        public ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default)
        {
            byte[]? buffer;
            using (var writer = new ArrayPoolBufferWriter())
            {
                MessageBuilder.WriteMessage(writer, key, message, options.MessagePackSerializerOptions);
                buffer = writer.WrittenSpan.ToArray();
            }

            var channel = Channel.CreateUnbounded<byte[]>();

            // TODO: channel?


            channel.Writer.TryWrite(buffer);
            return default;
        }
    }


    public class NamedPipeSubscriberCore<TKey, TMessage>
    {
        //readonly MessagePipeInProcessOptions options;

        //public void SubscribeAsync(IAsyncSubscriber<TKey, TMessage> subscriber)
        //{
        //    subscriber.Subscribe(
        //}
    }


    public class NamedPipeSubscriber<TKey, TMessage> : IDistributedSubscriber<TKey, TMessage>
        where TKey : notnull
    {
        readonly IAsyncPublisher<TKey, TMessage> inMemoryPublisher;
        readonly IAsyncSubscriber<TKey, TMessage> inMemorySubscriber;

        public NamedPipeSubscriber(IAsyncPublisher<TKey, TMessage> inMemoryPublisher, IAsyncSubscriber<TKey, TMessage> inMemorySubscriber)
        {
            this.inMemoryPublisher = inMemoryPublisher;
            this.inMemorySubscriber = inMemorySubscriber;

            
        }

        public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();

        }

        public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, MessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default)
        {
            var d = inMemorySubscriber.Subscribe(key, handler, filters);

            

            return new ValueTask<IAsyncDisposable>(new AsyncDisposableBridge(d));
        }
    }



    internal sealed class AsyncDisposableBridge : IAsyncDisposable
    {
        readonly IDisposable disposable;

        public AsyncDisposableBridge(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public ValueTask DisposeAsync()
        {
            disposable.Dispose();
            return default;
        }
    }

    internal sealed class AsyncMessageHandlerBridge<T> : IAsyncMessageHandler<T>
    {
        readonly IMessageHandler<T> handler;

        public AsyncMessageHandlerBridge(IMessageHandler<T> handler)
        {
            this.handler = handler;
        }

        public ValueTask HandleAsync(T message, CancellationToken cancellationToken)
        {
            handler.Handle(message);
            return default;
        }
    }

    internal sealed class AsyncMessageHandlerFilterBridge<T> : AsyncMessageHandlerFilter<T>
    {
        readonly MessageHandlerFilter<T> filter;

        public AsyncMessageHandlerFilterBridge(MessageHandlerFilter<T> filter)
        {
            this.filter = filter;
            this.Order = filter.Order;
        }

        public override ValueTask HandleAsync(T message, CancellationToken cancellationToken, Func<T, CancellationToken, ValueTask> next)
        {
            filter.Handle(message, x => next(x, cancellationToken));
            return default;
        }
    }

    internal class ArrayPoolBufferWriter : IBufferWriter<byte>, IDisposable
    {
        byte[] buffer;
        int index;

        const int MinBufferSize = 256;

        public int WrittenCount => index;
        public int Capacity => buffer.Length;
        public int FreeCapacity => buffer.Length - index;

        public ReadOnlySpan<byte> WrittenSpan => buffer.AsSpan(0, index);

        public ArrayPoolBufferWriter()
        {
            buffer = Array.Empty<byte>();
        }

        public void Advance(int count)
        {
            index += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return buffer.AsMemory(index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return buffer.AsSpan(index);
        }

        void EnsureCapacity(int sizeHint)
        {
            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (sizeHint > FreeCapacity)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(index + sizeHint, MinBufferSize));
                if (buffer.Length != 0)
                {
                    Array.Copy(buffer, 0, newBuffer, 0, index);
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                buffer = newBuffer;
            }
        }

        public void Dispose()
        {
            if (buffer.Length != 0)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                buffer = Array.Empty<byte>();
            }
        }
    }




    public static class MessageBuilder
    {
        // first
        public static void WriteMessage<TKey, TMessaege>(IBufferWriter<byte> buffer, TKey key, TMessaege message, MessagePackSerializerOptions options)
        {
            var writer = new MessagePackWriter(buffer);
            writer.WriteArrayHeader(3); // minFixArray
            // total length...
            // writer.WriteInt32
            

            MessagePackSerializer.Serialize(ref writer, key, options);
            MessagePackSerializer.Serialize(ref writer, message, options);
            writer.Flush();
        }

        public static InProcessMessage ReadMessage(byte[] buffer, MessagePackSerializerOptions options)
        {
            var reader = new MessagePackReader(buffer);
            if (reader.ReadArrayHeader() != 2)
            {
                throw new InvalidOperationException("Invalid messagepack buffer.");
            }

            var keyIndex = (int)reader.Consumed;
            reader.Skip();
            var keyOffset = (int)(reader.Consumed - keyIndex);
            reader.Skip();

            return new InProcessMessage(buffer, keyIndex, keyOffset);
        }
    }
}

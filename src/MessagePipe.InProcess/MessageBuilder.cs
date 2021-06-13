using MessagePack;
using MessagePipe.InProcess.Internal;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace MessagePipe.InProcess
{
    public interface IInProcessKey : IEquatable<IInProcessKey>
    {
        ReadOnlySpan<byte> KeySpan { get; }
    }

    public interface IInProcessValue
    {
        ReadOnlyMemory<byte> ValueMemory { get; }
    }

    internal sealed class InProcessMessage : IInProcessKey, IInProcessValue
    {
        readonly byte[] buffer;
        int keyIndex;
        readonly int keyOffset;
        readonly int hashCode;

        public InProcessMessage(byte[] buffer, int keyIndex, int keyOffset)
        {
            this.buffer = buffer;
            this.keyIndex = keyIndex;
            this.keyOffset = keyOffset;
            this.hashCode = CalcHashCode();
        }

        public ReadOnlySpan<byte> KeySpan => buffer.AsSpan(keyIndex, keyOffset - keyIndex);
        public ReadOnlyMemory<byte> ValueMemory => buffer.AsMemory(keyOffset, buffer.Length - keyOffset);

        public bool Equals(IInProcessKey? other)
        {
            if (other == null) return false;
            return KeySpan.SequenceEqual(other.KeySpan);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        int CalcHashCode()
        {
            // FNV1A32
            var obj = buffer;
            uint hash = 2166136261;
            for (int i = keyIndex; i < keyOffset; i++)
            {
                hash = unchecked((obj[i] ^ hash) * 16777619);
            }

            return unchecked((int)hash);
        }
    }

    internal static class MessageBuilder
    {
        // Message Frame-----
        // Length: msgpack-int32(5), without self(MsgPack Body Only)
        // Body(PubSub): MessagePack Array[3](Type(byte), Key, Value)

        const byte PubSubType = 1;
        const byte RequestResponseType = 2;

        public static IInProcessKey CreateKey<TKey>(TKey key, MessagePackSerializerOptions options)
        {
            var bytes = MessagePackSerializer.Serialize(key, options);
            return new InProcessMessage(bytes, 0, bytes.Length);
        }

        public static byte[] BuildPubSubMessage<TKey, TMessaege>(TKey key, TMessaege message, MessagePackSerializerOptions options)
        {
            using (var bufferWriter = new ArrayPoolBufferWriter())
            {
                var writer = new MessagePackWriter(bufferWriter);
                writer.WriteArrayHeader(3);
                writer.Write(PubSubType);
                MessagePackSerializer.Serialize(ref writer, key, options);
                MessagePackSerializer.Serialize(ref writer, message, options);
                writer.Flush();

                var finalBuffer = new byte[4 + bufferWriter.WrittenCount];
                Unsafe.WriteUnaligned(ref finalBuffer[0], bufferWriter.WrittenCount);
                bufferWriter.WrittenSpan.CopyTo(finalBuffer.AsSpan(4));
                return finalBuffer;
            }
        }

        public static int FetchMessageLength(ReadOnlySpan<byte> xs)
        {
            return Unsafe.ReadUnaligned<int>(ref Unsafe.AsRef(xs[0]));
        }

        public static InProcessMessage ReadPubSubMessage(byte[] buffer)
        {
            var reader = new MessagePackReader(buffer);
            if (reader.ReadArrayHeader() != 3)
            {
                throw new InvalidOperationException("Invalid messagepack buffer.");
            }

            if (reader.ReadByte() != PubSubType) // Read TypeCode
            {
                throw new InvalidOperationException("Invalid pubsub message type.");
            }

            var keyIndex = (int)reader.Consumed;
            reader.Skip();
            var keyOffset = (int)reader.Consumed;
            reader.Skip();

            return new InProcessMessage(buffer, keyIndex, keyOffset);
        }
    }
}

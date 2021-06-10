using MessagePack;
using System;
using System.Buffers;

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

        public ReadOnlySpan<byte> KeySpan => buffer.AsSpan(keyIndex, keyOffset- keyIndex);
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
        public static IInProcessKey CreateKey<TKey>(TKey key, MessagePackSerializerOptions options)
        {
            var bytes = MessagePackSerializer.Serialize(key, options);
            return new InProcessMessage(bytes, 0, bytes.Length);
        }

        public static void WriteMessage<TKey, TMessaege>(IBufferWriter<byte> buffer, TKey key, TMessaege message, MessagePackSerializerOptions options)
        {
            var writer = new MessagePackWriter(buffer);
            writer.WriteArrayHeader(2);
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
            var keyOffset = (int)(reader.Consumed - keyIndex + 1);
            reader.Skip();

            return new InProcessMessage(buffer, keyIndex, keyOffset);
        }
    }
}

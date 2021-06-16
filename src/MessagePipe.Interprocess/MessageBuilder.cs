using MessagePack;
using MessagePipe.Interprocess.Internal;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace MessagePipe.Interprocess
{
    public interface IInterprocessKey : IEquatable<IInterprocessKey>
    {
        ReadOnlyMemory<byte> KeyMemory { get; }
    }

    public interface IInterprocessValue
    {
        ReadOnlyMemory<byte> ValueMemory { get; }
    }

    internal sealed class InterprocessMessage : IInterprocessKey, IInterprocessValue
    {
        readonly byte[] buffer;
        int keyIndex;
        readonly int keyOffset;
        readonly int hashCode;

        public MessageType MessageType { get; }

        public InterprocessMessage(MessageType messageType, byte[] buffer, int keyIndex, int keyOffset)
        {
            this.MessageType = messageType;
            this.buffer = buffer;
            this.keyIndex = keyIndex;
            this.keyOffset = keyOffset;
            this.hashCode = CalcHashCode();
        }

        public ReadOnlyMemory<byte> KeyMemory => buffer.AsMemory(keyIndex, keyOffset - keyIndex);
        public ReadOnlyMemory<byte> ValueMemory => buffer.AsMemory(keyOffset, buffer.Length - keyOffset);

        public bool Equals(IInterprocessKey? other)
        {
            if (other == null) return false;
            return KeyMemory.Span.SequenceEqual(other.KeyMemory.Span);
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

    internal enum MessageType : byte
    {
        PubSub = 1,
        RemoteRequest = 2,
        RemoteResponse = 3,
        RemoteError = 4,
    }

    internal static class MessageBuilder
    {
        // Message Frame-----
        // Length: int32(4), without self(MsgPack Body Only)
        // Body(PubSub): MessagePack Array[3](Type(byte), key, message)
        // Body(Reques): MessagePack Array[3](Type(byte), (messageId:int, (reqType,resType):(string,string)), request)
        // Body(Respon): MessagePack Array[3](Type(byte), messageId:int, response)
        // Body(RError): MessagePack Array[3](Type(byte), messageId:int, error:string)

        public static IInterprocessKey CreateKey<TKey>(TKey key, MessagePackSerializerOptions options)
        {
            var bytes = MessagePackSerializer.Serialize(key, options);
            return new InterprocessMessage(MessageType.PubSub, bytes, 0, bytes.Length);
        }

        public static byte[] BuildPubSubMessage<TKey, TMessaege>(TKey key, TMessaege message, MessagePackSerializerOptions options)
        {
            using (var bufferWriter = new ArrayPoolBufferWriter())
            {
                var writer = new MessagePackWriter(bufferWriter);
                writer.WriteArrayHeader(3);
                writer.Write((byte)MessageType.PubSub);
                MessagePackSerializer.Serialize(ref writer, key, options);
                MessagePackSerializer.Serialize(ref writer, message, options);
                writer.Flush();

                var finalBuffer = new byte[4 + bufferWriter.WrittenCount];
                Unsafe.WriteUnaligned(ref finalBuffer[0], bufferWriter.WrittenCount);
                bufferWriter.WrittenSpan.CopyTo(finalBuffer.AsSpan(4));
                return finalBuffer;
            }
        }

        public static byte[] BuildRemoteRequestMessage<TRequest>(Type requestType, Type responseType, int messageId, TRequest message, MessagePackSerializerOptions options)
        {
            using (var bufferWriter = new ArrayPoolBufferWriter())
            {
                var writer = new MessagePackWriter(bufferWriter);
                writer.WriteArrayHeader(3);
                writer.Write((byte)MessageType.RemoteRequest);
                MessagePackSerializer.Serialize(ref writer, Tuple.Create(messageId, Tuple.Create(requestType.FullName, responseType.FullName)), options);
                MessagePackSerializer.Serialize(ref writer, message, options);
                writer.Flush();

                var finalBuffer = new byte[4 + bufferWriter.WrittenCount];
                Unsafe.WriteUnaligned(ref finalBuffer[0], bufferWriter.WrittenCount);
                bufferWriter.WrittenSpan.CopyTo(finalBuffer.AsSpan(4));
                return finalBuffer;
            }
        }

        public static byte[] BuildRemoteResponseMessage(int messageId, Type responseType, object message, MessagePackSerializerOptions options)
        {
            using (var bufferWriter = new ArrayPoolBufferWriter())
            {
                var writer = new MessagePackWriter(bufferWriter);
                writer.WriteArrayHeader(3);
                writer.Write((byte)MessageType.RemoteResponse);
                MessagePackSerializer.Serialize(ref writer, messageId, options);
                MessagePackSerializer.Serialize(responseType, ref writer, message, options);
                writer.Flush();

                var finalBuffer = new byte[4 + bufferWriter.WrittenCount];
                Unsafe.WriteUnaligned(ref finalBuffer[0], bufferWriter.WrittenCount);
                bufferWriter.WrittenSpan.CopyTo(finalBuffer.AsSpan(4));
                return finalBuffer;
            }
        }

        public static byte[] BuildRemoteResponseError(int messageId, string exception, MessagePackSerializerOptions options)
        {
            using (var bufferWriter = new ArrayPoolBufferWriter())
            {
                var writer = new MessagePackWriter(bufferWriter);
                writer.WriteArrayHeader(3);
                writer.Write((byte)MessageType.RemoteError);
                MessagePackSerializer.Serialize(ref writer, messageId, options);
                MessagePackSerializer.Serialize(ref writer, exception, options);
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

        public static InterprocessMessage ReadPubSubMessage(byte[] buffer)
        {
            var reader = new MessagePackReader(buffer);
            if (reader.ReadArrayHeader() != 3)
            {
                throw new InvalidOperationException("Invalid messagepack buffer.");
            }

            var b = reader.ReadByte();
            var msgType = (MessageType)b;

            var keyIndex = (int)reader.Consumed;
            reader.Skip();
            var keyOffset = (int)reader.Consumed;
            reader.Skip();

            return new InterprocessMessage(msgType, buffer, keyIndex, keyOffset);
        }
    }
}

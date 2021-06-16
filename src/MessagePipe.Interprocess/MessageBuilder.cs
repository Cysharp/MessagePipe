using MessagePack;
using MessagePack.Formatters;
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
        readonly int keyIndex;
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
        // Body(Reques): MessagePack Array[3](Type(byte), RequestHeader, request)
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
                MessagePackSerializer.Serialize(ref writer, new RequestHeader(messageId, requestType.FullName!, responseType.FullName!), options);
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

    // (messageId:int, (reqType,resType):(string,string))

    [Preserve]
    [MessagePackFormatter(typeof(Formatter))]
    internal class RequestHeader
    {
        public int MessageId { get; }
        public string RequestType { get; }
        public string ResponseType { get; }

        public RequestHeader(int messageId, string requestType, string responseType)
        {
            MessageId = messageId;
            RequestType = requestType;
            ResponseType = responseType;
        }

        [Preserve]
        public class Formatter : IMessagePackFormatter<RequestHeader>
        {
            public RequestHeader Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                // debugging...
                var x = reader.ReadArrayHeader();
                if (x != 3) throw new MessagePack.MessagePackSerializationException("Array length is invalid. Length:" + x);
                var id = reader.ReadInt32();

                var req = reader.ReadString();
                var res = reader.ReadString();
                return new RequestHeader(id, req, res);
            }

            public void Serialize(ref MessagePackWriter writer, RequestHeader value, MessagePackSerializerOptions options)
            {
                writer.WriteArrayHeader(3);
                writer.Write(value.MessageId);
                writer.Write(value.RequestType);
                writer.Write(value.ResponseType);
            }
        }
    }
}

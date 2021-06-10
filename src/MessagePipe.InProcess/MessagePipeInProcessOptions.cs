using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MessagePipe.InProcess
{
    public interface INamedPipeStreamFactory
    {
        public ValueTask<NamedPipeServerStream> GetServerStreamAsync();
        public ValueTask<NamedPipeClientStream> GetClientStreamAsync();
    }

    public sealed class MessagePipeInProcessOptions
    {
        public INamedPipeStreamFactory NamedPipeStreamFactory { get; }
        public MessagePackSerializerOptions MessagePackSerializerOptions { get; }

        public MessagePipeInProcessOptions(INamedPipeStreamFactory namedPipeStreamFactory)
        {
            this.MessagePackSerializerOptions = ContractlessStandardResolver.Options;
            this.NamedPipeStreamFactory = namedPipeStreamFactory;
        }

        public MessagePipeInProcessOptions(INamedPipeStreamFactory namedPipeStreamFactory, MessagePackSerializerOptions options)
        {
            this.MessagePackSerializerOptions = options;
            this.NamedPipeStreamFactory = namedPipeStreamFactory;
        }
    }

    public sealed class NamedPipeMessagingWorker : IDisposable
    {
        IAsyncPublisher<IInProcessKey, IInProcessValue> publisher;
        IAsyncSubscriber<IInProcessKey, IInProcessValue> subscriber;


        NamedPipeClientStream? clientStream; 
        NamedPipeServerStream? serverStream;

        public NamedPipeMessagingWorker()
        {
            var server = new NamedPipeServerStream("PIPE");
            
        }

        //async void NamedPipeMessagingWorker()
        //{
        //    // NamedPipeClientStream writer;

        //    NamedPipeServerStream serverStream = null;
        //    var buffered = new BufferedStream(serverStream, 4096);

        //    //buffered.Flush()


        //    // serverStream.ReadMode = PipeTransmissionMode.Message;
        //    // serverStream.ReadByte();


        //    //var reader = PipeReader.Create(serverStream);

        //    var tako = ReadFullyAsync(null!, null!);

            
            


        //    // get the first bytes
        //    // reader.ReadAsync(

        //    byte[] headerBuffer = new byte[5];
        //    var readLength = await serverStream.ReadAsync(headerBuffer, 0, 5);


        //    // i.Buffer.End

        //    // new MessagePackReader(i.Buffer);

        //    // publisher.Publish(
        //    await Task.Yield();


        //    var writeChannel = Channel.CreateUnbounded<byte[]>();
        //    var readChannel = Channel.CreateUnbounded<byte[]>();
        //}

        void Write(byte[] message)
        {
            
        }

        public void Get(IServiceProvider provider)
        {
            NamedPipeClientStream stream;

            //var msg = MessageBuilder.ReadMessage(null, null);




            // publisher.Publish(msg, msg);





        }

        static byte[] ReadFullyAsync(byte[] initialBuffer, NamedPipeServerStream stream)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public interface IInProcessKey : IEquatable<IInProcessKey>
    {
        ReadOnlySpan<byte> KeySpan { get; }
    }

    public interface IInProcessValue
    {
        ReadOnlySpan<byte> ValueSpan { get; }
    }

    public sealed class InProcessMessage : IInProcessKey, IInProcessValue
    {
        readonly byte[] buffer;
        int keyIndex;
        readonly int keyOffset;

        public InProcessMessage(byte[] buffer, int keyIndex, int keyOffset)
        {
            this.buffer = buffer;
            this.keyIndex = keyIndex;
            this.keyOffset = keyOffset;
        }

        public ReadOnlySpan<byte> KeySpan => buffer.AsSpan(keyIndex, keyOffset);
        public ReadOnlySpan<byte> ValueSpan => buffer.AsSpan(keyOffset, buffer.Length - keyOffset);

        public bool Equals(IInProcessKey other)
        {
            return KeySpan.SequenceEqual(other.KeySpan);
        }

        public override int GetHashCode()
        {
            return 0; // todo:hash
        }
    }


    public class BufferSizeConfigurableUdpServer : IDisposable
    {
        Socket socket;
        byte[] buffer;
        const int MinBuffer = 4096;

        public BufferSizeConfigurableUdpServer(int port, int bufferSize)
        {
            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveBufferSize = bufferSize;
            socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            buffer = new byte[Math.Max(bufferSize, MinBuffer)];
        }

        public async Task<ReadOnlyMemory<byte>> ReceiveAsync()
        {
            var i = await socket.ReceiveAsync(buffer, SocketFlags.None);
            return buffer.AsMemory(0, i);
        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }

    public class BufferSizeConfigurableUdpClient : IDisposable
    {
        Socket socket;
        byte[] buffer;
        const int MinBuffer = 4096;

        public BufferSizeConfigurableUdpClient(string host, int port, int bufferSize)
        {
            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            socket.SendBufferSize = bufferSize;
            socket.Connect(new IPEndPoint(IPAddress.Parse(host), port));
            buffer = new byte[Math.Max(bufferSize, MinBuffer)];
        }

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}
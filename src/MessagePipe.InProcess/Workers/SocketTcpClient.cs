using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe.InProcess.Workers
{
    internal sealed class SocketTcpServer : IDisposable
    {
        const int MinBuffer = 4096;

        readonly Socket socket;
        readonly byte[] buffer;

        SocketTcpServer(int bufferSize)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveBufferSize = bufferSize;
            buffer = new byte[Math.Max(bufferSize, MinBuffer)];
        }

        public static SocketTcpServer Bind(int port, int bufferSize)
        {
            var server = new SocketTcpServer(bufferSize);
            server.socket.Bind(new IPEndPoint(IPAddress.Any, port));
            return server;
        }

        public async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken)
        {
#if NET5_0_OR_GREATER
            var i = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            return buffer.AsMemory(0, i);
#else
            var tcs = new TaskCompletionSource<ReadOnlyMemory<byte>>();

            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, x =>
            {
                int i;
                try
                {
                    i = socket.EndReceive(x);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                    return;
                }
                var r = buffer.AsMemory(0, i);
                tcs.TrySetResult(r);
            }, null);

            return await tcs.Task;
#endif
        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }

    internal sealed class SocketTcpClient : IDisposable
    {
        const int MinBuffer = 4096;

        readonly Socket socket;
        readonly byte[] buffer;

        SocketTcpClient(int bufferSize)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SendBufferSize = bufferSize;
            buffer = new byte[Math.Max(bufferSize, MinBuffer)];
        }

        public static SocketTcpClient Connect(string host, int port, int bufferSize)
        {
            var client = new SocketTcpClient(bufferSize);
            client.socket.Connect(new IPEndPoint(IPAddress.Parse(host), port));
            return client;
        }

        public ValueTask<int> SendAsync(byte[] buffer, CancellationToken cancellationToken = default)
        {
#if NET5_0_OR_GREATER
            return socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
#else
            var tcs = new TaskCompletionSource<int>();
            socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, x =>
             {
                 int i;
                 try
                 {
                     i = socket.EndSend(x);
                 }
                 catch (Exception ex)
                 {
                     tcs.TrySetException(ex);
                     return;
                 }
                 tcs.TrySetResult(i);
             }, null);
            return new ValueTask<int>(tcs.Task);
#endif
        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}
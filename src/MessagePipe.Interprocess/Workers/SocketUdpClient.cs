using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe.Interprocess.Workers
{
    internal sealed class SocketUdpServer : IDisposable
    {
        const int MinBuffer = 4096;

        readonly Socket socket;
        readonly byte[] buffer;

        SocketUdpServer(int bufferSize, AddressFamily addressFamily, ProtocolType protocolType)
        {
            socket = new Socket(addressFamily, SocketType.Dgram, protocolType);
            socket.ReceiveBufferSize = bufferSize;
            buffer = new byte[Math.Max(bufferSize, MinBuffer)];
        }
        public static SocketUdpServer Bind(int port, int bufferSize)
        {
            var server = new SocketUdpServer(bufferSize, AddressFamily.InterNetwork, ProtocolType.Udp);
            server.socket.Bind(new IPEndPoint(IPAddress.Any, port));
            return server;
        }
#if NET5_0_OR_GREATER
        /// <summary>
        /// create UDP socket and bind for listen.
        /// </summary>
        /// <param name="domainSocketPath">path to socket</param>
        /// <param name="bufferSize">socket buffer size</param>
        /// <exception cref="SocketException">unix domain socket not supported or socket already exists even if it is not bound</exception>
        /// <returns>UDP server with bound socket</returns>
        public static SocketUdpServer BindUds(string domainSocketPath, int bufferSize)
        {
            var server = new SocketUdpServer(bufferSize, AddressFamily.Unix, ProtocolType.IP);
            server.socket.Bind(new UnixDomainSocketEndPoint(domainSocketPath));
            return server;
        }
#endif

        public async ValueTask<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken)
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

    internal sealed class SocketUdpClient : IDisposable
    {
        const int MinBuffer = 4096;

        readonly Socket socket;
        readonly byte[] buffer;

        SocketUdpClient(int bufferSize, AddressFamily addressFamily, ProtocolType protocolType)
        {
            socket = new Socket(addressFamily, SocketType.Dgram, protocolType);
            socket.SendBufferSize = bufferSize;
            buffer = new byte[Math.Max(bufferSize, MinBuffer)];
        }

        public static SocketUdpClient Connect(string host, int port, int bufferSize)
        {
            var ipaddr = IPAddress.Parse(host);
            var client = new SocketUdpClient(bufferSize, ipaddr.AddressFamily, ProtocolType.Udp);
            client.socket.Connect(new IPEndPoint(ipaddr, port));
            return client;
        }
#if NET5_0_OR_GREATER
        /// <summary>
        /// create UDP unix domain socket client and connect to server
        /// </summary>
        /// <param name="domainSocketPath">path to unix domain socket</param>
        /// <param name="bufferSize"></param>
        /// <exception cref="SocketException">unix domain socket not supported or server does not exist</exception>
        /// <returns>UDP unix domain socket client</returns>
        public static SocketUdpClient ConnectUds(string domainSocketPath, int bufferSize)
        {
            var client = new SocketUdpClient(bufferSize, AddressFamily.Unix, ProtocolType.IP);
            client.socket.Connect(new UnixDomainSocketEndPoint(domainSocketPath));
            return client;
        }
#endif

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
#if !UNITY_2018_3_OR_NEWER
            return new ValueTask<int>(tcs.Task);
#else
            return tcs.Task;
#endif
#endif
        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}
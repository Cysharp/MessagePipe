using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessagePipe.Sandbox.ConsoleApp
{
    public class NapedPipeTest
    {
        public async static Task Main()
        {
            string host = "127.0.0.1";
            int port = 1245;
            var ip = new IPEndPoint(IPAddress.Parse(host), port);

            var server = new NamedPipeServerStream("foo", PipeDirection.InOut, 1);
            var client = new NamedPipeClientStream(".", "foo", PipeDirection.InOut);

            await client.ConnectAsync();
            await server.WaitForConnectionAsync();
            
            var buffer = new byte[1024];
            var r = server.ReadAsync(buffer);
            client.Dispose(); // disconnected

            Console.WriteLine("WAIT FOREVER?");
            var i = await r;

            Console.WriteLine("OK?" + i); // 0 is ok
            await server.ReadAsync(buffer);
            Console.WriteLine("WAIT");

            server.Dispose();

            

            var newServer = new NamedPipeServerStream("foo", PipeDirection.InOut, 1);
            var newClient = new NamedPipeClientStream(".", "foo", PipeDirection.InOut);

            await newClient.ConnectAsync();
            await newServer.WaitForConnectionAsync();



        }

        static async Task ServerLoop(Socket server, TaskCompletionSource tcs)
        {
            var remote = await server.AcceptAsync();
            remote.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            remote.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.TcpKeepAliveTime, 4);

            // await tcs.Task;    

            var buffer = new byte[1024];
            try
            {
                var get = await remote.ReceiveAsync(buffer, SocketFlags.None); // 0 is disconnect?
            }
            catch
            {
                Console.WriteLine("EX");
            }



        }
    }
}

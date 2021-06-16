using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessagePipe.Sandbox.ConsoleApp
{
    public class TcpTest
    {
        public async static Task Main2()
        {
            string host = "127.0.0.1";
            int port = 1245;
            var ip = new IPEndPoint(IPAddress.Parse(host), port);

            var server = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.ReceiveTimeout = 1000;
            server.NoDelay = true;

            server.Bind(ip);
            server.Listen(10);

            var tcs = new TaskCompletionSource();
            _ = ServerLoop(server, tcs);

            var client = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(ip);



            Console.ReadLine();

            Console.WriteLine("DISCONNECT");
            //client.Disconnect(false);



            /*
            Console.WriteLine("START SEND");

            var byte1 = Encoding.UTF8.GetBytes("foo");
            var byte2 = Encoding.UTF8.GetBytes("bar");
            client.Send(byte1, byte1.Length, SocketFlags.None);
            client.Send(byte2, byte2.Length, SocketFlags.None);



            tcs.TrySetResult();
            */

            Console.ReadLine();
            await Task.Yield();
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

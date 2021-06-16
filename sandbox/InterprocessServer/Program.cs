using ConsoleAppFramework;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InterprocessServer
{
    class Program : ConsoleAppBase
    {
        static void Main(string[] args)
        {
            //var buffer = new byte[] { 147, 1, 172, 83, 121, 115, 116, 101, 109, 46, 73, 110, 116, 51, 50, 173, 83, 121, 115, 116, 101, 109, 46, 83, 116, 114, 105, 110, 103 };
            //var bbbuffer = new byte[] { 147, 1, 172, 83, 121, 115, 116, 101, 109, 46, 73, 110, 116, 51, 50, 173, 83, 121, 115, 116, 101, 109, 46, 83, 116, 114, 105, 110, 103 }.AsMemory();
            //var tako = MessagePackSerializer.Deserialize<RequestHeader>(bbbuffer, ContractlessStandardResolver.Options);






            var id = Guid.NewGuid();
            //var isServer = args[0] == "SERVER";
            // var isServer = true;
            // Console.WriteLine(args[0]);

            Host.CreateDefaultBuilder()
                .ConfigureServices(x =>
                {
                    x.AddSingleton<GuidHolder>(x => new GuidHolder { guid = id });
                    x.AddMessagePipe();

                    x.AddMessagePipeTcpInterprocess("127.0.0.1", 1232, xx =>
                     {
                         xx.HostAsServer = true;
                     });
                })
                .RunConsoleAppFrameworkAsync<Program>(args);
        }
        IRemoteRequestHandler<int, string> handler;
        Guid guid;

        public Program(IRemoteRequestHandler<int, string> handler, GuidHolder guid)
        {
            this.handler = handler;
            this.guid = guid.guid;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("GO INVOKE");
            var v = await handler.InvokeAsync(9999);
            Console.WriteLine("NOGO INVOKE");
            Console.WriteLine(v);
        }
    }

    public class GuidHolder
    {
        public Guid guid;
    }

    public class MyRequestHandler : IAsyncRequestHandler<Guid, string>
    {

        GuidHolder guid;
        public MyRequestHandler(GuidHolder guid)
        {
            this.guid = guid;
        }

        public async ValueTask<string> InvokeAsync(Guid request, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return "CLIENT:" + request + ", SERVER:" + guid.guid;
        }
    }


    public class MyAsyncHandler2 : IAsyncRequestHandler<int, string>
    {
        public async ValueTask<string> InvokeAsync(int request, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1);
            if (request == -1)
            {
                throw new Exception("NO -1");
            }
            else
            {
                return "ECHO:" + request.ToString();
            }
        }
    }

    //[Preserve]
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
        
        //[Preserve]
        public class Formatter : IMessagePackFormatter<RequestHeader>
        {
            public RequestHeader Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                // debugging...
                var x = reader.ReadArrayHeader();
                Console.WriteLine(x);
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

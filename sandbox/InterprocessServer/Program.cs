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






            //var isServer = args[0] == "SERVER";
            // var isServer = true;
            // Console.WriteLine(args[0]);

            Host.CreateDefaultBuilder()
                .ConfigureServices(x =>
                {
                    x.AddMessagePipe();
                    x.AddMessagePipeNamedPipeInterprocess("messagepipe-pipe");
                })
                .RunConsoleAppFrameworkAsync<Program>(args);
        }

        public async Task RunA(IDistributedPublisher<string, int> publisher)
        {
            await publisher.PublishAsync("foo", 100);
        }

        public async Task RunB(IDistributedSubscriber<string, int> subscriber)
        {
            await subscriber.SubscribeAsync("foo", x =>
            {
                Console.WriteLine(x);
            });
        }



        public async Task RunClient(IRemoteRequestHandler<int, string> handler)
        {
            var v = await handler.InvokeAsync(100);
            Console.WriteLine(v); // "ECHO:100"
        }


    }


    public class MyAsyncHandler : IAsyncRequestHandler<int, string>
    {
        public async ValueTask<string> InvokeAsync(int request, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1);
            return "ECHO:" + request.ToString();
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

using ConsoleAppFramework;
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
            var id = Guid.NewGuid();
            var isServer = args[0] == "SERVER";
            Console.WriteLine(args[0]);

            Host.CreateDefaultBuilder()
                .ConfigureServices(x =>
                {
                    x.AddSingleton<GuidHolder>(x => new GuidHolder { guid = id });
                    x.AddMessagePipe();

                    x.AddMessagePipeNamedPipeInterprocess("mypipe", x =>
                    {
                        // x.AsServer = isServer;
                    });
                })
                .RunConsoleAppFrameworkAsync<Program>(args);
        }
        IRemoteRequestHandler<Guid, string> handler;
        Guid guid;

        public Program(IRemoteRequestHandler<Guid, string> handler, GuidHolder guid)
        {
            this.handler = handler;
            this.guid = guid.guid;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("GO:");
            Console.ReadLine();
            var result = await handler.InvokeAsync(guid);
            Console.WriteLine(result);
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
}

using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe.Sandbox.ConsoleApp
{
    public class MyStreamRequestHandler : IStreamRequestHandler<MyRequest, MyResponse>
    {
        public async IAsyncEnumerable<MyResponse> HandleAsync(MyRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < 3; i++)
            {
                await Task.Delay(100, cancellationToken);
                yield return new MyResponse { Value = $"Response {i} for {request.Value}" };
            }
        }
    }

    public class MyRequest { public string Value { get; set; } }
    public class MyResponse { public string Value { get; set; } }

    class StreamDemo
    {
        public static async Task RunAsync(IServiceProvider provider)
        {
            var handler = provider.GetRequiredService<IStreamRequestHandler<MyRequest, MyResponse>>();
            var request = new MyRequest { Value = "Test" };
            var cts = new CancellationTokenSource();
            await foreach (var response in handler.HandleAsync(request, cts.Token))
            {
                Debug.WriteLine($"Received Response: {response.Value}");
                Console.WriteLine($"Received Response: {response.Value}");
            }
        }
    }
}

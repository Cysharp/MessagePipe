using System;
using System.Threading.Tasks;

namespace MessagePipe.Benchmark
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new PublishOps().MeasureAllAsync();
        }
    }
}

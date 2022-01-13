using System;
using BenchmarkDotNet.Running;

namespace MessagePipe.Interprocess.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            new BenchmarkSwitcher(typeof(Program).Assembly).Run();
        }
    }
}

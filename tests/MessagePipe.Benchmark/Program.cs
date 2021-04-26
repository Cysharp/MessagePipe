using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MessagePipe.Benchmark
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new PublishOps().MeasureAllAsync();

            //BenchmarkSwitcher.FromAssembly(Assembly.GetEntryAssembly()).Run(args);
            await Task.Yield();
        }
    }

    internal class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.ShortRun.WithWarmupCount(1).WithIterationCount(1).WithRuntime(CoreRuntime.Core50));

            // Add Targetframeworks net47 to csproj(removed for CI)
            //AddJob(Job.ShortRun.WithWarmupCount(1).WithIterationCount(1).WithRuntime(ClrRuntime.Net47));
        }
    }
}

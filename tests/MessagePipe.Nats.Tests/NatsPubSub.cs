using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlterNats;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MessagePipe.Nats.Tests;

public class NatsPubSub
{
    [Theory]
    [MemberData(nameof(BasicTestData))]
    public async Task Basic<T>(IEnumerable<T> items)
    {
        var provider = TestHelper.BuildNatsServiceProvider();
        var p = provider.GetRequiredService<IDistributedPublisher<NatsKey, T>>();
        var s = provider.GetRequiredService<IDistributedSubscriber<NatsKey, T>>();

        AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        autoResetEvent.Reset();
        List<T> results = new();
        var natsKey = new NatsKey(Guid.NewGuid().ToString("N"));

        await using var d = await s.SubscribeAsync(natsKey, x =>
        {
            results.Add(x);

            if (results.Count == items.Count())
                autoResetEvent.Set();
        });

        foreach (var item in items)
        {
            await p.PublishAsync(natsKey, item);
        }

        var waitResult = autoResetEvent.WaitOne(5000);

        Assert.True(waitResult, "Timeout");
        Assert.Equal(items.ToArray(), results.ToArray());
    }

    static readonly int[] seed1 = { 24, 45, 99, 41, 98, 7, 81, 8, 26, 56 };

    static object[][] BasicTestData()
    {
        return new[]
        {
            new object[] { seed1 },
            new object[] { seed1.Select(x => $"Test:{x}") },
            new object[] { seed1.Select(x => new SampleClass(x, $"Name{x}")) }
        };
    }
}

public class SampleClass : IEquatable<SampleClass>
{
    public int Id { get; set; }
    public string Name { get; set; }

    public SampleClass(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public bool Equals(SampleClass? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id && Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((SampleClass)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }

    public override string ToString()
    {
        return $"{Id}-{Name}";
    }
}

public static class TestHelper
{
    public static IServiceProvider BuildNatsServiceProvider(INatsSerializer? serializer = default)
    {
        var sc = new ServiceCollection();
        sc.AddMessagePipe();

        if (serializer == default)
        {
            sc.AddMessagePipeNats(new NatsConnectionFactory());
        }
        else
        {
            sc.AddMessagePipeNats(new NatsConnectionFactory(NatsOptions.Default with
            {
                Serializer = serializer
            }));
        }

        return sc.BuildServiceProvider();
    }
}
# MessagePipe

High performance in-memory/distributed messaging pipeline for .NET and Unity.

* DI first
* filter pipeline
* sync/async
* broadcast/response(+many)
* in-memory/distributed
* keyed/keyless

Howto
---
```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe(options => { });
        // services.AddMessagePipeRedis();
    })
```

```csharp
var publisher = provider.GetRequiredService<IPublisher<int>>();
var subscriber = provider.GetRequiredService<ISubscriber<int>>();

var d = subscriber.Subscribe(x => Console.WriteLine(x));

publisher.Publish(10);
publisher.Publish(20);
publisher.Publish(30);

d.Dispose();
```

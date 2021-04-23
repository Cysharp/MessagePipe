# MessagePipe

MessagePipe is a high-performance in-memory/distributed messaging pipeline for .NET and Unity. It supports all cases of Pub/Sub usage, mediator pattern for CQRS, EventAggregator of Prism(V-VM decoupling), etc.

* Dependency-injection first
* Filter pipeline
* sync/async
* keyed/keyless
* broadcast/response(+many)
* in-memory/distributed

/* TODO:Graph */


Getting Started
---
For .NET, use NuGet. For Unity, please read [Unity](#unity) section.

> PM> Install-Package [MessagePipe](https://www.nuget.org/packages/MessagePipe)

MessagePipe is built on top of a `Microsoft.Extensions.DependencyInjection`(for Unity, `VContainer` or `Zenject`) so set up via `ConfigureServices`.

```csharp
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe(options => { });
    })
```

Get the `IPublisher<T>` for publisher, Get the `ISubscribe<T>` for subscriber.


 `T`

```csharp
using MessagePipe;

public struct MyEvent { }

public class PageA
{
    readonly IPublisher<MyEvent> publisher;
    
    public PageA(IPublisher<MyEvent> publisher)
    {
        this.publisher = publisher;
    }

    void Send()
    {
        this.publisher.Publish(new MyEvent());
    }
}

public class PageB
{
    readonly ISubscriber<MyEvent> subscriber;
    readonly IDisposable disposable;

    public PageB(ISubscriber<MyEvent> subscriber)
    {
        var bag = DisposableBag.CreateBuilder(); // composite disposable for manage subscription
        
        subscriber.Subscribe(x => Console.WriteLine("here")).AddTo(bag);

        disposable = bag.Build();
    }

    void Close()
    {
        disposable.Dispose(); // unsubscribe event
    }
}
```











Publish/Subscribe
---


with predicate.


Request/Response/All
---



Filter
---


sync(`MessageHandlerFilter<T>`) and async(`AsyncMessageHandlerFilter<>T`) and request(`RequestHandlerFilter<TReq, TRes>`) and async request (`AsyncRequestHandlerFilter<TReq, TRes>`).




These are idea showcase of filter.

```csharp
public class ChangedValueFilter<T> : MessageHandlerFilter<T>
{
    T lastValue;

    public override void Handle(T message, Action<T> next)
    {
        if (EqualityComparer<T>.Default.Equals(message, lastValue))
        {
            return;
        }

        lastValue = message;
        next(message);
    }
}
```

```csharp
public class PredicateFilter<T> : MessageHandlerFilter<T>
{
    public PredicateFilter(Func<T, bool> predicate)
    {
        this.predicate = predicate;
    }

    public override void Handle(T message, Action<T> next)
    {
        if (predicate(message))
        {
            next(message);
        }
    }
}
```

```csharp
public class LockFilter<T> : MessageHandlerFilter<T>
{
    readonly object gate = new object();

    public override void Handle(T message, Action<T> next)
    {
        lock (gate)
        {
            next(message);
        }
    }
}
```

```csharp
public class IgnoreErrorFilter<T> : MessageHandlerFilter<T>
{
    readonly ILogger<IgnoreErrorFilter<T>> logger;

    public IgnoreErrorFilter(ILogger<IgnoreErrorFilter<T>> logger)
    {
        this.logger = logger;
    }

    public override void Handle(T message, Action<T> next)
    {
        try
        {
            next(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ""); // error logged, but do not propagate
        }
    }
}
```

```csharp
public class DispatcherFilter<T> : MessageHandlerFilter<T>
{
    readonly Dispatcher dispatcher;

    public DispatcherFilter(Dispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    public override void Handle(T message, Action<T> next)
    {
        dispatcher.BeginInvoke(() =>
        {
            next(message);
        });
    }
}
```

```csharp
public class DelayFilter<T> : AsyncMessageHandlerFilter<T>
{
    readonly TimeSpan delaySpan;

    public DelayFilter(TimeSpan delaySpan)
    {
        this.delaySpan = delaySpan;
    }

    public override async ValueTask HandleAsync(T message, CancellationToken cancellationToken, Func<T, CancellationToken, ValueTask> next)
    {
        await Task.Delay(delaySpan, cancellationToken);
        await next(message, cancellationToken);
    }
}
```


Managing Subscription
---


MessagePipe.Redis / IDistributedPubSub
---





MessagePipeOptions
---


// AutoRegistration


Diagnostics
---


Integration with other DI library
---



Unity
---
For Unity, you can choose [VContainer](https://github.com/hadashiA/VContainer/) or [Zenject](https://github.com/modesttree/Zenject) for runtime.

// TODO: Install path

Unity version does not have open generics support(for IL2CPP) and does not support auto registration. Therefore, all required types need to be manually registered.

VContainer's installation sample.

```csharp
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // RegisterMessagePipe returns options.
        var options = builder.RegisterMessagePipe(/* configure option */);
        
        // RegisterMessageBroker: Register for IPublisher<T>/ISubscriber<T>
        builder.RegisterMessageBroker<int>(options);

        // also exists RegisterAsyncMessageBroker, RegisterRequestHandler, RegisterAsyncRequestHandler

        // RegisterHandlerFilter: Register for filter, also exists RegisterAsyncMessageHandlerFilter, Register(Async)RequestHandlerFilter
        builder.RegisterMessageHandlerFilter<MyFilter<int>>();


        builder.RegisterEntryPoint<MessagePipeDemo>(Lifetime.Singleton);
    }
}

public class MessagePipeDemo : VContainer.Unity.IStartable
{
    readonly IPublisher<int> publisher;
    readonly ISubscriber<int> subscriber;

    public MessagePipeDemo(IPublisher<int> publisher, ISubscriber<int> subscriber)
    {
        this.publisher = publisher;
        this.subscriber = subscriber;
    }

    public void Start()
    {
        var d = DisposableBag.CreateBuilder();
        subscriber.Subscribe(x => Debug.Log("S1:" + x)).AddTo(d);
        subscriber.Subscribe(x => Debug.Log("S2:" + x)).AddTo(d);

        publisher.Publish(10);
        publisher.Publish(20);
        publisher.Publish(30);

        var disposable = d.Build();
        disposable.Dispose();
    }
}
```

Zenject's installation sample.

```csharp
void Configure(DiContainer builder)
{
    // BindMessagePipe returns options.
    var options = builder.BindMessagePipe(/* configure option */);
    
    // BindMessageBroker: Register for IPublisher<T>/ISubscriber<T>
    builder.BindMessageBroker<int>(options);

    // also exists BindAsyncMessageBroker, BindRequestHandler, BindAsyncRequestHandler

    // BindHandlerFilter: Bind for filter, also exists BindAsyncMessageHandlerFilter, Bind(Async)RequestHandlerFilter
    builder.BindMessageHandlerFilter<MyFilter<int>>();


    builder.BindEntryPoint<MessagePipeDemo>(Lifetime.Singleton);
}
```

> Zenject version is not supported `InstanceScope.Singleton` for Zenject's limitation. The default is `Scoped`, which cannot be changed.

Adding global filter, you can not use open generics filter so recommended to create these helper method.

```csharp
// Register IPublisher<T>/ISubscriber<T> and global filter.
static void RegisterMessageBroker<T>(IContainerBuilder builder, MessagePipeOptions options)
{
    builder.RegisterMessageBroker<T>(options);

    // setup for global filters.
    options.AddGlobalMessageHandlerFilter<MyMessageHandlerFilter<T>>();
}

// Register IRequestHandler<TReq, TRes>/IRequestAllHandler<TReq, TRes> and global filter.
static void RegisterRequest<TRequest, TResponse, THandler>(IContainerBuilder builder, MessagePipeOptions options)
    where THandler : IRequestHandler
{
    builder.RegisterRequestHandler<TRequest, TResponse, THandler>(options);
    
    // setup for global filters.
    options.AddGlobalRequestHandlerFilter<MyRequestHandlerFilter<TRequest, TResponse>>();
}
```

License
---
This library is licensed under the MIT License.
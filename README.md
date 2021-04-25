# MessagePipe

MessagePipe is a high-performance in-memory/distributed messaging pipeline for .NET and Unity. It supports all cases of Pub/Sub usage, mediator pattern for CQRS, EventAggregator of Prism(V-VM decoupling), etc.

* Dependency-injection first
* Filter pipeline
* sync/async
* keyed/keyless
* broadcast/response(+many)
* in-memory/distributed

MessagePipe is faster than standard C# event and 78 times faster than Prism's EventAggregator.

![](https://user-images.githubusercontent.com/46207/115984507-5d36da80-a5e2-11eb-9942-66602906f499.png)

Of course, memory allocation per publish operation is less(zero).

![](https://user-images.githubusercontent.com/46207/115814615-62542800-a430-11eb-9041-1f31c1ac8464.png)

Getting Started
---
For .NET, use NuGet. For Unity, please read [Unity](#unity) section.

> PM> Install-Package [MessagePipe](https://www.nuget.org/packages/MessagePipe)

MessagePipe is built on top of a `Microsoft.Extensions.DependencyInjection`(for Unity, `VContainer` or `Zenject`) so set up via `ConfigureServices` in [.NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host). Generic Host is widely used in .NET such as ASP.NET Core, [MagicOnion](https://github.com/Cysharp/MagicOnion/), [ConsoleAppFramework](https://github.com/Cysharp/ConsoleAppFramework/), MAUI, WPF(with external support), etc so easy to setup.

```csharp
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe(); // AddMessagePipe(options => { }) for configure options
    })
```

Get the `IPublisher<T>` for publisher, Get the `ISubscribe<T>` for subscriber, like a `Logger<T>`. `T` can be any type, primitive(int, string, etc...), struct, class, enum, etc.

```csharp
using MessagePipe;

public struct MyEvent { }

public class SceneA
{
    readonly IPublisher<MyEvent> publisher;
    
    public SceneA(IPublisher<MyEvent> publisher)
    {
        this.publisher = publisher;
    }

    void Send()
    {
        this.publisher.Publish(new MyEvent());
    }
}

public class SceneB
{
    readonly ISubscriber<MyEvent> subscriber;
    readonly IDisposable disposable;

    public SceneB(ISubscriber<MyEvent> subscriber)
    {
        var bag = DisposableBag.CreateBuilder(); // composite disposable for manage subscription
        
        subscriber.Subscribe(x => Console.WriteLine("here")).AddTo(bag);

        disposable = bag.Build();
    }

    void Close()
    {
        disposable.Dispose(); // unsubscribe event, all subscription **must** Dispose when completed
    }
}
```

It is similar to event, but decoupled by type as key. The return value of Subscribe is `IDisposable`, which makes it easier to unsubscribe than event. You can release many subscriptions at once by `DisposableBag`(`CompositeDisposable`). See the [Managing Subscription and Diagnostics](#managing-subscription-and-diagnostics) section for more details.

The publisher/subscriber(internally we called MessageBroker) is managed by DI, it is possible to have different broker for each scope. Also, all subscriptions are unsubscribed when the scope is disposed, which prevents subscription leaks.

> Default is singleton, you can configure `MessagePipeOptions.InstanceLifetime` to `Singleton` or `Scoped`.

`IPublisher<T>/ISubscriber<T>` is keyless(type only) however MessagePipe has similar interface `IPublisher<TKey, TMessage>/ISubscriber<TKey, TMessage>` that is keyed(topic) interface.

For example, our real usecase,  // TODO sentence.
// TODO: Image

```csharp
// MagicOnion(similar as SignalR, realtime event framework for .NET and Unity)
public class UnityConnectionHub : StreamingHubBase<IUnityConnectionHub, IUnityConnectionHubReceiver>, IUnityConnectionHub
{
    readonly IPublisher<Guid, UnitEventData> eventPublisher;
    readonly IPublisher<Guid, ConnectionClose> closePublisher;
    Guid id;

    public UnityConnectionHub(IPublisher<Guid, UnitEventData> eventPublisher, IPublisher<Guid, ConnectionClose> closePublisher)
    {
        this.eventPublisher = eventPublisher;
        this.closePublisher = closePublisher;
    }

    override async ValueTask OnConnected()
    {
        this.id = Guid.Parse(Context.Headers["id"]);
    }

    override async ValueTask OnDisconnected()
    {
        this.closePublisher.Publish(id, new ConnectionClose()); // publish to browser(Blazor)
    }

    // called from Client(Unity)
    public Task<UnityEventData> SendEventAsync(UnityEventData data)
    {
        this.eventPublisher.Publish(id, data); // publish to browser(Blazor)
    }
}

// Blazor
public partial class BlazorPage : ComponentBase, IDisposable
{
    [Parameter]
    public Guid ID { get; set; }

    [Inject]
    ISubscriber<Guid, UnitEventData> UnityEventSubscriber { get; set; }

    [Inject]
    ISubscriber<Guid, ConnectionClose> ConnectionCloseSubscriber { get; set; }

    IDisposable subscription;

    protected override void OnInitialized()
    {
        // receive event from MagicOnion(that is from Unity)
        var d1 = UnityEventSubscriber.Subscribe(ID, x =>
        {
            // do anything...
        });

        var d2 = ConnectionCloseSubscriber.Subscribe(ID, _ =>
        {
            // show disconnected thing to view...
            subscription?.Dispose(); // and unsubscribe events.
        });

        subscription = DisposableBag.Create(d1, d2); // combine disposable.
    }
    
    public void Dispose()
    {
        // unsubscribe event when browser is closed.
        subscription?.Dispose();
    }
}
```

> The main difference of Reactive Extensions' Subject is has no `OnCompleted`. OnCompleted may or may not be used, making it very difficult to determine the intent to the observer(subscriber). Also, we usually subscribe to multiple events from the same (different event type)publisher, and it is difficult to handle duplicate OnCompleted in that case. For this reason, MessagePipe only provides a simple Publish(OnNext). If you want to convey completion, please receive a separate event and perform dedicated processing there.






TODO: mediator
https://docs.microsoft.com/ja-jp/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api#implement-the-command-and-command-handler-patterns




async, await all.




request/response.



filter









Publish/Subscribe
---


keyed(topic) and keyless interface.




```csharp
public interface IPublisher<TMessage>
{
    void Publish(TMessage message);
}

public interface ISubscriber<TMessage>
{
    public IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters);
}
```


with key







Request/Response/All
---



Subscribe Extensions
---

with predicate
AsObservable





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


Managing Subscription and Diagnostics
---

better than event.

TODO: example of Blazor.







> Weak reference, which is widely used in WPF, is an anti-pattern. All subscriptions should be managed explicitly, and `DisposableBag` (`CompositeDisposable`) can help with that.






MessagePipe.Redis / IDistributedPubSub
---

// inmemory provider



MessagePipeOptions
---
You can configure MessagePipe behaviour by `MessagePipeOptions`.




```csharp
public sealed class MessagePipeOptions
{
    AsyncPublishStrategy DefaultAsyncPublishStrategy; // default is Parallel
    HandlingSubscribeDisposedPolicy HandlingSubscribeDisposedPolicy; // default is Ignore
    InstanceLifetime InstanceLifetime; // default is Singleton
    bool EnableAutoRegistration;  // default is true
    bool EnableCaptureStackTrace; // default is false

    void AddGlobal***Filter<T>();
}

public enum AsyncPublishStrategy
{
    Sequential, Parallel
}

public enum InstanceLifetime
{
    Singleton, Scoped
}

public enum HandlingSubscribeDisposedPolicy
{
    Ignore, Throw
}
```

### AsyncPublishStrategy

### HandlingSubscribeDisposedPolicy

### InstanceLifetime

### EnableAutoRegistration


### EnableCaptureStackTrace


### AddGlobal***Filter


Integration with other DI library
---
All(popular) DI libraries has `Microsoft.Extensions.DependencyInjection` bridge so configure by MS.E.DI and use bridge if you want.

Compare with Channels
---
[System.Threading.Channels](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels)(for Unity, `UniTask.Channels`) uses Queue internal, the producer is not affected by the performance of the consumer, and the consumer can control the flow rate(back pressure). This is a different use than MessagePipe's Pub/Sub.

Unity
---
You need to install Core library and choose [VContainer](https://github.com/hadashiA/VContainer/) or [Zenject](https://github.com/modesttree/Zenject) for runtime. You can install via UPM git URL package or asset package(MessagePipe.*.unitypackage) available in [MessagePipe/releases](https://github.com/Cysharp/MessagePipe/releases) page.

* Core `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe`
* VContainer `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.VContainer`
* Zenject `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.Zenject`

Andalso, requires [UniTask](https://github.com/Cysharp/UniTask) to install.

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
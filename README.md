# MessagePipe
[![GitHub Actions](https://github.com/Cysharp/MessagePipe/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/MessagePipe/actions) [![Releases](https://img.shields.io/github/release/Cysharp/MessagePipe.svg)](https://github.com/Cysharp/MessagePipe/releases)

MessagePipe is a high-performance in-memory/distributed messaging pipeline for .NET and Unity. It supports all cases of Pub/Sub usage, mediator pattern for CQRS, EventAggregator of Prism(V-VM decoupling), IPC(Interprocess Communication)-RPC, etc.

* Dependency-injection first
* Filter pipeline
* better event
* sync/async
* keyed/keyless
* buffered/bufferless
* singleton/scoped
* broadcast/response(+many)
* in-memory/interprocess/distributed

MessagePipe is faster than standard C# event and 78 times faster than Prism's EventAggregator.

![](https://user-images.githubusercontent.com/46207/115984507-5d36da80-a5e2-11eb-9942-66602906f499.png)

Of course, memory allocation per publish operation is less(zero).

![](https://user-images.githubusercontent.com/46207/115814615-62542800-a430-11eb-9041-1f31c1ac8464.png)

Also providing roslyn-analyzer to prevent subscription leak.

![](https://user-images.githubusercontent.com/46207/117535259-da753d00-b02f-11eb-9818-0ab5ef3049b1.png)

Getting Started
---
For .NET, use NuGet. For Unity, please read [Unity](#unity) section.

> PM> Install-Package [MessagePipe](https://www.nuget.org/packages/MessagePipe)

MessagePipe is built on top of a `Microsoft.Extensions.DependencyInjection`(for Unity, `VContainer` or `Zenject` or `Builtin Tiny DI`) so set up via `ConfigureServices` in [.NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host). Generic Host is widely used in .NET such as ASP.NET Core, [MagicOnion](https://github.com/Cysharp/MagicOnion/), [ConsoleAppFramework](https://github.com/Cysharp/ConsoleAppFramework/), MAUI, WPF(with external support), etc so easy to setup.

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

For example, our real usecase, There is an application that connects Unity and [MagicOnion](https://github.com/Cysharp/MagicOnion/) (a real-time communication framework like SignalR) and delivers it via a browser by Blazor. At that time, we needed something to connect Blazor's page (Browser lifecycle) and MagicOnion's Hub (Connection lifecycle) to transmit data. We also need to distribute the connections by their IDs.

`Browser <-> Blazor <- [MessagePipe] -> MagicOnion <-> Unity`

We solved this with the following code.

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

> In other words, this is the equivalent of [Relay in RxSwift](https://github.com/ReactiveX/RxSwift/blob/main/Documentation/Subjects.md).

In addition to standard Pub/Sub, MessagePipe supports async handlers, mediator patterns with handlers that accept return values, and filters for pre-and-post execution customization.

This image is a visualization of the connection between all those interfaces.

![image](https://user-images.githubusercontent.com/46207/122254092-bf87c980-cf07-11eb-8bdd-039c87309db6.png)

You may be confused by the number of interfaces, but many functions can be written with a similar, unified API.

Publish/Subscribe
---
Publish/Subscribe interface has keyed(topic) and keyless, sync and async interface.

```csharp
// keyless-sync
public interface IPublisher<TMessage>
{
    void Publish(TMessage message);
}

public interface ISubscriber<TMessage>
{
    IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters);
}

// keyless-async
public interface IAsyncPublisher<TMessage>
{
    // async interface's publish is fire-and-forget
    void Publish(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default(CancellationToken));
}

public interface IAsyncSubscriber<TMessage>
{
    IDisposable Subscribe(IAsyncMessageHandler<TMessage> asyncHandler, params AsyncMessageHandlerFilter<TMessage>[] filters);
}

// keyed-sync
public interface IPublisher<TKey, TMessage>
    where TKey : notnull
{
    void Publish(TKey key, TMessage message);
}

public interface ISubscriber<TKey, TMessage>
    where TKey : notnull
{
    IDisposable Subscribe(TKey key, IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters);
}

// keyed-async
public interface IAsyncPublisher<TKey, TMessage>
    where TKey : notnull
{
    void Publish(TKey key, TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default(CancellationToken));
    ValueTask PublishAsync(TKey key, TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default(CancellationToken));
}

public interface IAsyncSubscriber<TKey, TMessage>
    where TKey : notnull
{
    IDisposable Subscribe(TKey key, IAsyncMessageHandler<TMessage> asyncHandler, params AsyncMessageHandlerFilter<TMessage>[] filters);
}
```

All are available in the form of `IPublisher/Subscribe<T>` in the DI. async handler can await all subscribers completed by `await PublishAsync`. Asynchronous methods can work sequentially or in parallel, depending on `AsyncPublishStrategy` (defaults is `Parallel`, can be changed by `MessagePipeOptions` or by specifying at publish time). If you don't need to wait, you can call `void Publish` to act as fire-and-forget.

The before and after of execution can be changed by passing a custom filter. See the [Filter](#filter) section for details.

If an error occurs, it will be propagated to the caller and subsequent subscribers will be stopped. This behavior can be changed by writing a filter to ignore errors.

ISingleton***, IScoped***
---
I(Async)Publisher(Subscriber)'s lifetime belongs to `MessagePipeOptions.InstanceLifetime`. However if declare with `ISingletonPublisher<TMessage>`/`ISingletonSubscriber<TKey, TMessage>`, `ISingletonAsyncPublisher<TMessage>`/`ISingletonAsyncSubscriber<TKey, TMessage>` then used singleton lifetime. Also `IScopedPublisher<TMessage>`/`IScopedSubscriber<TKey, TMessage>`, `IScopedAsyncPublisher<TMessage>`/`IScopedAsyncSubscriber<TKey, TMessage>` uses scoped lifetime.

Buffered
---
`IBufferedPublisher<TMessage>/IBufferedSubscriber<TMessage>` pair is similar as `BehaviorSubject` or Reactive Extensions(More equal is RxSwift's `BehaviorRelay`). It returns latest value on `Subscribe`.

```csharp
var p = provider.GetRequiredService<IBufferedPublisher<int>>();
var s = provider.GetRequiredService<IBufferedSubscriber<int>>();

p.Publish(999);

var d1 = s.Subscribe(x => Console.WriteLine(x)); // 999
p.Publish(1000); // 1000

var d2 = s.Subscribe(x => Console.WriteLine(x)); // 1000
p.Publish(9999); // 9999, 9999

DisposableBag.Create(d1, d2).Dispose();
```

> If `TMessage` is class and does not have latest value(null), does not send value on Subscribe.

> Keyed buffered publisher/subscriber does not exist because difficult to avoid memory leak of (unused)key and keep latest value.

EventFactory
---
Using `EventFactory`, you can create generic `IPublisher/ISubscriber`, `IAsyncPublisher/IAsyncSubscriber`, `IBufferedPublisher/IBufferedSubscriber`, `IBufferedAsyncPublisher/IBufferedAsyncSubscriber` like C# events, with a Subscriber tied to each instance, not grouped by type.

MessagePipe has better properties than a normal C# event

* Using Subscribe/Dispose instead of `+=`, `-=` , easy to management subscription
* Both sync and async support
* Both bufferless and buffered support
* Enable unsubscribe all subscription from publisher.dispose
* Attaches invocation pipeline behaviour by Filter
* To monitor subscription leak by `MessagePipeDiagnosticsInfo`
* TO prevent subscription leak by `MessagePipe.Analyzer`

```csharp
public class BetterEvent : IDisposable
{
    // using MessagePipe instead of C# event/Rx.Subject
    // store Publisher to private field(declare IDisposablePublisher/IDisposableAsyncPublisher)
    IDisposablePublisher<int> tickPublisher;

    // Subscriber is used from outside so public property
    public ISubscriber<int> OnTick { get; }

    public BetterEvent(EventFactory eventFactory)
    {
        // CreateEvent can deconstruct by tuple and set together
        (tickPublisher, OnTick) = eventFactory.CreateEvent<int>();

        // also create async event(IAsyncSubscriber) by `CreateAsyncEvent`
        // eventFactory.CreateAsyncEvent
    }

    int count;
    void Tick()
    {
        tickPublisher.Publish(count++);
    }

    public void Dispose()
    {
        // You can unsubscribe all from Publisher.
        tickPublisher.Dispose();
    }
}
```

If you want to create event outside of DI, see [Global Provider](#global-provider) section.

```csharp
IDisposablePublisher<int> tickPublisher;
public ISubscriber<int> OnTick { get; }

ctor()
{
    (tickPublisher, OnTick) = GlobalMessagePipe.CreateEvent<int>();
}
```

Request/Response/All
---
Similar as [MediatR](https://github.com/jbogard/MediatR), implement support of mediator pattern.

```csharp
public interface IRequestHandler<in TRequest, out TResponse>
{
    TResponse Invoke(TRequest request);
}

public interface IAsyncRequestHandler<in TRequest, TResponse>
{
    ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default);
}
```

For example, declare handler for Ping type.

```csharp
public readonly struct Ping { }
public readonly struct Pong { }

public class PingPongHandler : IRequestHandler<Ping, Pong>
{
    public Pong Invoke(Ping request)
    {
        Console.WriteLine("Ping called.");
        return new Pong();
    }
}
```

You can get handler like this.

```csharp
class FooController
{
    IRequestHandler<Ping, Pong> requestHandler;

    // automatically instantiate PingPongHandler.
    public FooController(IRequestHandler<Ping, Pong> requestHandler)
    {
        this.requestHandler = requestHandler;
    }

    public void Run()
    {
        var pong = this.requestHandler.Invoke(new Ping());
        Console.WriteLine("PONG");
    }
}
```

For more complex implementation patterns, [this Microsoft documentation](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api#implement-the-command-process-pipeline-with-a-mediator-pattern-mediatr) is applicable.

Declare many request handlers, you can use `IRequestAllHandler`, `IAsyncRequestAllHandler` instead of single handler.

```csharp
public interface IRequestAllHandler<in TRequest, out TResponse>
{
    TResponse[] InvokeAll(TRequest request);
    IEnumerable<TResponse> InvokeAllLazy(TRequest request);
}

public interface IAsyncRequestAllHandler<in TRequest, TResponse>
{
    ValueTask<TResponse[]> InvokeAllAsync(TRequest request, CancellationToken cancellationToken = default);
    ValueTask<TResponse[]> InvokeAllAsync(TRequest request, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TResponse> InvokeAllLazyAsync(TRequest request, CancellationToken cancellationToken = default);
}
```

```csharp
public class PingPongHandler1 : IRequestHandler<Ping, Pong>
{
    public Pong Invoke(Ping request)
    {
        Console.WriteLine("Ping1 called.");
        return new Pong();
    }
}

public class PingPongHandler2 : IRequestHandler<Ping, Pong>
{
    public Pong Invoke(Ping request)
    {
        Console.WriteLine("Ping1 called.");
        return new Pong();
    }
}

class BarController
{
    IRequestAllHandler<Ping, Pong> requestAllHandler;

    public FooController(IRequestAllHandler<Ping, Pong> requestAllHandler)
    {
        this.requestAllHandler = requestAllHandler;
    }

    public void Run()
    {
        var pongs = this.requestAllHandler.InvokeAll(new Ping());
        Console.WriteLine("PONG COUNT:" + pongs.Length);
    }
}
```

Subscribe Extensions
---
`ISubscriber`(`IAsyncSubscriber`) interface requires `IMessageHandler<T>` to handle message.

```csharp
public interface ISubscriber<TMessage>
{
    IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters);
}
```

However, the extension method allows you to write `Action<T>` directly.

```csharp
public static IDisposable Subscribe<TMessage>(this ISubscriber<TMessage> subscriber, Action<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters)
public static IDisposable Subscribe<TMessage>(this ISubscriber<TMessage> subscriber, Action<TMessage> handler, Func<TMessage, bool> predicate, params MessageHandlerFilter<TMessage>[] filters)
public static IObservable<TMessage> AsObservable<TMessage>(this ISubscriber<TMessage> subscriber, params MessageHandlerFilter<TMessage>[] filters)
public static IAsyncEnumerable<TMessage> AsAsyncEnumerable<TMessage>(this IAsyncSubscriber<TMessage> subscriber, params AsyncMessageHandlerFilter<TMessage>[] filters)
public static ValueTask<TMessage> FirstAsync<TMessage>(this ISubscriber<TMessage> subscriber, CancellationToken cancellationToken, params MessageHandlerFilter<TMessage>[] filters)
public static ValueTask<TMessage> FirstAsync<TMessage>(this ISubscriber<TMessage> subscriber, CancellationToken cancellationToken, Func<TMessage, bool> predicate, params MessageHandlerFilter<TMessage>[] filters)
```

Also, the `Func<TMessage, bool>` overload can filter messages by predicate (internally implemented with PredicateFilter, where Order is int.MinValue and is always checked first).

`AsObservable` can convert message pipeline to `IObservable<T>`, it can handle by Reactive Extensions(in Unity, you can use `UniRx`). `AsObservable` exists in sync subscriber(keyless, keyed, buffered).

`AsAsyncEnumerable` can convert message pipeline to `IAsyncEnumerable<T>`, it can handle by async LINQ and async foreach. `AsAsyncEnumerable` exists in async subscriber(keyless, keyed, buffered).

`FirstAsync` gets the first value of message. It is similar as `AsObservable().FirstAsync()`, `AsObservable().Where().FirstAsync()`. If uses `CancellationTokenSource(TimeSpan)` then similar as `AsObservable().Timeout().FirstAsync()`. Argument of `CancellationToken` is required to avoid task leak. 

```csharp
// for Unity, use cts.CancelAfterSlim(TIimeSpan) instead.
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
var value = await subscriber.FirstAsync(cts.Token);
```

`FirstAsync` exists in both sync and async subscriber(keyless, keyed, buffered).

Filter
---
Filter system can hook before and after method invocation. It is implemented with the Middleware pattern, which allows you to write synchronous and asynchronous code with similar syntax. MessagePipe provides different filter types - sync (`MessageHandlerFilter<T>`), async (`AsyncMessageHandlerFilter<T>`), request (`RequestHandlerFilter<TReq, TRes>`) and async request (`AsyncRequestHandlerFilter<TReq, TRes>`). To implement other concerete filters the above filter types can be extended.

Filters can be specified in three places - global(by `MessagePipeOptions.AddGlobalFilter`), per handler type, and per subscription. These filters are sorted according to the Order specified in each of them, and are generated when subscribing.

Since the filter is generated on a per subscription basis, the filter can have a state.

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

// uses(per subscribe)
subscribe.Subscribe(x => Console.WriteLine(x), new ChangedValueFilter<int>(){ Order = 100 });

// add per handler type(use generics filter, write open generics)
[MessageHandlerFilter(typeof(ChangedValueFilter<>), 100)]
public class WriteLineHandler<T> : IMessageHandler<T>
{
    public void Handle(T message) => Console.WriteLine(message);
}

// add per global
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe(options =>
        {
            options.AddGlobalMessageHandlerFilter(typeof(ChangedValueFilter<>), 100);
        });
    });
```

use the filter by attribute, you can use these attributes: `[MessageHandlerFilter(type, order)]`, `[AsyncMessageHandlerFilter(type, order)]`, `[RequestHandlerFilter(type, order)]`, `[AsyncRequestHandlerFilter(type, order)]`.

These are idea showcase of filter.

```csharp
public class PredicateFilter<T> : MessageHandlerFilter<T>
{
    private readonly Func<T, bool> predicate;

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
public class DelayRequestFilter : AsyncRequestHandlerFilter<int, int>
{
    public override async ValueTask<int> InvokeAsync(int request, CancellationToken cancellationToken, Func<int, CancellationToken, ValueTask<int>> next)
    {
        await Task.Delay(TimeSpan.FromSeconds(request));
        var response = await next(request, cancellationToken);
        return response;
    }
}
```

Managing Subscription and Diagnostics
---
Subscribe returns `IDisposable`; when call `Dispose` then unsubscribe. A better reason than event is that it is easy to Unsubscribe. To manage multiple IDisposable, you can use `CompositeDisposable` in Rx(UniRx) or `DisposableBag` included in MessagePipe.

```csharp
IDisposable disposable;

void OnInitialize(ISubscriber<int> subscriber)
{
    var d1 = subscriber.Subscribe(_ => { });
    var d2 = subscriber.Subscribe(_ => { });
    var d3 = subscriber.Subscribe(_ => { });

    // static DisposableBag: DisposableBag.Create(1~7(optimized) or N);
    disposable = DisposableBag.Create(d1, d2, d3);
}

void Close()
{
    // dispose all subscription
    disposable?.Dispose();
}
```

```csharp
IDisposable disposable;

void OnInitialize(ISubscriber<int> subscriber)
{
    // use builder pattern, you can use subscription.AddTo(bag)
    var bag = DisposableBag.CreateBuilder();

    subscriber.Subscribe(_ => { }).AddTo(bag);
    subscriber.Subscribe(_ => { }).AddTo(bag);
    subscriber.Subscribe(_ => { }).AddTo(bag);

    disposable = bag.Build(); // create final composite IDisposable
}

void Close()
{
    // dispose all subscription
    disposable?.Dispose();
}
```

```csharp
IDisposable disposable;

void OnInitialize(ISubscriber<int> subscriber)
{
    var bag = DisposableBag.CreateBuilder();

    // calling once(or x count), you can use DisposableBag.CreateSingleAssignment to hold subscription reference.
    var d = DisposableBag.CreateSingleAssignment();
    
    // you can invoke Dispose in handler action.
    // assign disposable, you can use `SetTo` and `AddTo` bag.
    // or you can use d.Disposable = subscriber.Subscribe();
    subscriber.Subscribe(_ => { d.Dispose(); }).SetTo(d).AddTo(bag);

    disposable = bag.Build();
}

void Close()
{
    disposable?.Dispose();
}
```

The returned `IDisposable` value **must** be handled. If it is ignored, it will leak. However Weak reference, which is widely used in WPF, is an anti-pattern. All subscriptions should be managed explicitly.

You can monitor subscription count by `MessagePipeDiagnosticsInfo`. It can get from service provider(or DI).

```csharp
public sealed class MessagePipeDiagnosticsInfo
{
    /// <summary>Get current subscribed count.</summary>
    public int SubscribeCount { get; }

    /// <summary>
    /// When MessagePipeOptions.EnableCaptureStackTrace is enabled, list all stacktrace on subscribe.
    /// </summary>
    public StackTraceInfo[] GetCapturedStackTraces(bool ascending = true);

    /// <summary>
    /// When MessagePipeOptions.EnableCaptureStackTrace is enabled, groped by caller of subscribe.
    /// </summary>
    public ILookup<string, StackTraceInfo> GetGroupedByCaller(bool ascending = true)
}
```

If you monitor SubscribeCount, you can check leak of subscription.

```csharp
public class MonitorTimer : IDisposable
{
    CancellationTokenSource cts = new CancellationTokenSource();

    public MonitorTimer(MessagePipeDiagnosticsInfo diagnosticsInfo)
    {
        RunTimer(diagnosticsInfo);
    }

    async void RunTimer(MessagePipeDiagnosticsInfo diagnosticsInfo)
    {
        while (!cts.IsCancellationRequested)
        {
            // show SubscribeCount
            Console.WriteLine("SubscribeCount:" + diagnosticsInfo.SubscribeCount);
            await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
        }
    }

    public void Dispose()
    {
        cts.Cancel();
    }
}
```

Also, by enabling MessagePipeOptions.EnableCaptureStackTrace (disabled by default), the location of the subscribed location can be displayed, making it easier to find the location of the leak if it exists.

Check the Count of GroupedByCaller, and if any of them show abnormal values, then the stack trace is where it occurs, and you probably ignore Subscription.

for Unity, `Window ->  MessagePipe Diagnostics` window is useful for monitoring subscritpion. It visualizes `MessagePipeDianogsticsInfo`.

![image](https://user-images.githubusercontent.com/46207/116953319-e2e41580-acc7-11eb-88c9-a4704bf3e3c9.png)

To Enable use of the MessagePipeDiagnostics window, require to set up `GlobalMessagePipe`.

```csharp
// VContainer
public class MessagePipeDemo : VContainer.Unity.IStartable
{
    public MessagePipeDemo(IObjectResolver resolver)
    {
        // require this line.
        GlobalMessagePipe.SetProvider(resolver.AsServiceProvider());
    }
}

// Zenject
void Configure(DiContainer container)
{
    GlobalMessagePipe.SetProvider(container.AsServiceProvider());
}

// builtin
var prodiver = builder.BuildServiceProvider();
GlobalMessagePipe.SetProvider(provider);
```

Analyzer
---
In previous section, we anounce `The returned IDisposable value **must** be handled`. To prevent subscription leak, we provide roslyn analyzer.

> PM> Install-Package [MessagePipe.Analyzer](https://www.nuget.org/packages/MessagePipe.Analyzer)

![](https://user-images.githubusercontent.com/46207/117535259-da753d00-b02f-11eb-9818-0ab5ef3049b1.png)

This will raise an error for unhandled `Subscribe`.

This analyzer can use after Unity 2020.2(see: [Roslyn analyzers and ruleset files](https://docs.unity3d.com/2020.2/Documentation/Manual/roslyn-analyzers.html) document). `MessagePipe.Analyzer.dll` exists in [releases page](https://github.com/Cysharp/MessagePipe/releases/).

![](https://user-images.githubusercontent.com/46207/117535248-d5b08900-b02f-11eb-8add-33101a71033a.png)

Currently Unity's analyzer support is incomplete. We are complementing analyzer support with editor extension, please check the [Cysharp/CsprojModifier](https://github.com/Cysharp/CsprojModifier).

![](https://github.com/Cysharp/CsprojModifier/raw/master/docs/images/Screen-01.png)

IDistributedPubSub / MessagePipe.Redis
---
For the distributed(networked) Pub/Sub, you can use `IDistributedPublisher<TKey, TMessage>`, `IDistributedSubscriber<TKey, TMessage>` instead of `IAsyncPublisher`.

```csharp
public interface IDistributedPublisher<TKey, TMessage>
{
    ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default);
}

public interface IDistributedSubscriber<TKey, TMessage>
{
    // and also without filter overload.
    public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IMessageHandler<TMessage> handler, MessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default);
    public ValueTask<IAsyncDisposable> SubscribeAsync(TKey key, IAsyncMessageHandler<TMessage> handler, AsyncMessageHandlerFilter<TMessage>[] filters, CancellationToken cancellationToken = default);
}
```

`IAsyncPublisher` means in-memory Pub/Sub. Since processing over the network is fundamentally different, you need to use a different interface to avoid confusion.

Redis is available as a standard network provider.

> PM> Install-Package [MessagePipe.Redis](https://www.nuget.org/packages/MessagePipe.Redis)

use `AddMessagePipeRedis` to enable redis provider.

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe()
            .AddRedis(IConnectionMultiplexer | IConnectionMultiplexerFactory, configure);
    })
```

`IConnectionMultiplexer` overload, you can pass [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)'s `ConnectionMultiplexer` directly. Implement own `IConnectionMultiplexerFactory` to allow for per-key distribution and use from connection pools.

`MessagePipeRedisOptions`, you can configure serialization.

```csharp
public sealed class MessagePipeRedisOptions
{
    public IRedisSerializer RedisSerializer { get; set; }
}

public interface IRedisSerializer
{
    byte[] Serialize<T>(T value);
    T Deserialize<T>(byte[] value);
}
```

In default uses [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp)'s `ContractlessStandardResolver`. You can change to use other `MessagePackSerializerOptions` by `new MessagePackRedisSerializer(options)` or implement own serializer wrapper.

MessagePipe has in-memory IDistributedPublisher/Subscriber for local test usage.

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration.Get<MyConfig>();

        var builder = services.AddMessagePipe();
        if (config.IsLocal)
        {
            // use in-memory IDistributedPublisher/Subscriber in local.
            builder.AddInMemoryDistributedMessageBroker();   
        }
        else
        {
            // use Redis IDistributedPublisher/Subscriber
            builder.AddRedis();
        }
    });
```

InterprocessPubSub, IRemoteAsyncRequest / MessagePipe.Interprocess
---
For the interprocess(NamedPipe/UDP/TCP) Pub/Sub(IPC), you can use `IDistributedPublisher<TKey, TMessage>`, `IDistributedSubscriber<TKey, TMessage>` similar as `MessagePipe.Redis`.

> PM> Install-Package MessagePipe.Interprocess

MessagePipe.Interprocess is also exsits on Unity(except NamedPipe).

use `AddUdpInterprocess`, `AddTcpInterprocess`, `AddNamedPipeInterprocess`, `AddUdpInterprocessUds`, `AddTcpInterprocessUds` to enable interprocess provider(Uds is Unix domain socket, most performant option).

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe()
            .AddUdpInterprocess("127.0.0.1", 3215, configure); // setup host and port.
            // .AddTcpInterprocess("127.0.0.1", 3215, configure);
            // .AddNamedPipeInterprocess("messagepipe-namedpipe", configure);
            // .AddUdpInterprocessUds("domainSocketPath")
            // .AddTcpInterprocessUds("domainSocketPath")
    })
```

```csharp
public async P(IDistributedPublisher<string, int> publisher)
{
    // publish value to remote process.
    await publisher.PublishAsync("foobar", 100);
}

public async S(IDistributedSubscriber<string, int> subscriber)
{
    // subscribe remote-message with "foobar" key.
    await subscriber.SubscribeAsync("foobar", x =>
    {
        Console.WriteLine(x);
    });
}
```

when injected `IDistributedPublisher`, process will be `server`, start to listen client. when injected `IDistributedSubscriber`, process will be `client`, start to connect to server. when DI scope is closed, server/client connection is closed.

Udp is connectionless protocol so does not require server is started before client connect. However protocol limitation, does not send over 64K message. We're recommend to use this if message is not large.

Namedpipe is 1:1 connection, can not connect multiple subscribers.

Tcp has no such restrictions and is the most flexible of all the options.

In default uses [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp)'s `ContractlessStandardResolver` for message serialization. You can change to use other `MessagePackSerializerOptions` by MessagePipeInterprocessOptions.MessagePackSerializerOptions.

```csharp
builder.AddUdpInterprocess("127.0.0.1", 3215, options =>
{
    // You can configure other options, `InstanceLifetime` and `UnhandledErrorHandler`.
    options.MessagePackSerializerOptions = StandardResolver.Options;
});
```

For IPC-RPC, you can use `IRemoteRequestHandler<in TRequest, TResponse>` that invoke remote `IAsyncRequestHandler<TRequest, TResponse>`. using `TcpInterprocess` or `NamedPipeInterprocess` enabled it.

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe()
            .AddTcpInterprocess("127.0.0.1", 3215, x =>
            {
                x.HostAsServer = true; // if remote process as server, set true(otherwise false(default)).
            });
    });
```

```csharp
// example: server handler
public class MyAsyncHandler : IAsyncRequestHandler<int, string>
{
    public async ValueTask<string> InvokeAsync(int request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1);
        if (request == -1)
        {
            throw new Exception("NO -1");
        }
        else
        {
            return "ECHO:" + request.ToString();
        }
    }
}
```

```csharp
// client
async void A(IRemoteRequestHandler<int, string> remoteHandler)
{
    var v = await remoteHandler.InvokeAsync(9999);
    Console.WriteLine(v); // ECHO:9999
}
```

For Unity, requires to import MessagePack-CSharp package and needs slightly different configuration.

```csharp
// example of VContainer
var builder = new ContainerBuilder();
var options = builder.RegisterMessagePipe(configure);

var messagePipeBuilder = builder.ToMessagePipeBuilder(); // require to convert ServiceCollection to enable Intereprocess

var interprocessOptions = messagePipeBuilder.AddTcpInterprocess();

// register manually.
// IDistributedPublisher/Subscriber
messagePipeBuilder.RegisterTcpInterprocessMessageBroker<int, int>(interprocessOptions);
// RemoteHandler
builder.RegisterAsyncRequestHandler<int, string, MyAsyncHandler>(options); // for server
messagePipeBuilder.RegisterTcpRemoteRequestHandler<int, string>(interprocessOptions); // for client
```

MessagePipeOptions
---
You can configure MessagePipe behaviour by `MessagePipeOptions` in `AddMessagePipe(Action<MMessagePipeOptions> configure)`.

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        // var config = ctx.Configuration.Get<MyConfig>(); // optional: get settings from configuration(use it for options configure)

        services.AddMessagePipe(options =>
        {
            options.InstanceLifetime = InstanceLifetime.Scoped;
#if DEBUG
            // EnableCaptureStackTrace slows performance, so recommended to use only in DEBUG and in profiling, disable it.
            options.EnableCaptureStackTrace = true;
#endif
        });
    })
```

Option has these properties(and method).

```csharp
public sealed class MessagePipeOptions
{
    AsyncPublishStrategy DefaultAsyncPublishStrategy; // default is Parallel
    HandlingSubscribeDisposedPolicy HandlingSubscribeDisposedPolic; // default is Ignore
    InstanceLifetime InstanceLifetime; // default is Singleton
    InstanceLifetime RequestHandlerLifetime; // default is Scoped
    bool EnableAutoRegistration;  // default is true
    bool EnableCaptureStackTrace; // default is false

    void SetAutoRegistrationSearchAssemblies(params Assembly[] assemblies);
    void SetAutoRegistrationSearchTypes(params Type[] types);
    void AddGlobal***Filter<T>();
}

public enum AsyncPublishStrategy
{
    Parallel, Sequential
}

public enum InstanceLifetime
{
    Singleton, Scoped, Transient
}

public enum HandlingSubscribeDisposedPolicy
{
    Ignore, Throw
}
```

### DefaultAsyncPublishStrategy

`IAsyncPublisher` has `PublishAsync` method. If AsyncPublishStrategy.Sequential, await each subscribers. If Parallel, uses WhenAll.

```csharp
public interface IAsyncPublisher<TMessage>
{
    // using Default AsyncPublishStrategy
    ValueTask PublishAsync(TMessage message, CancellationToken cancellationToken = default);
    ValueTask PublishAsync(TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default);
    // snip others...
}

public interface IAsyncPublisher<TKey, TMessage>
    where TKey : notnull
{
    // using Default AsyncPublishStrategy
    ValueTask PublishAsync(TKey key, TMessage message, CancellationToken cancellationToken = default);
    ValueTask PublishAsync(TKey key, TMessage message, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default);
    // snip others...
}

public interface IAsyncRequestAllHandler<in TRequest, TResponse>
{
    // using Default AsyncPublishStrategy
    ValueTask<TResponse[]> InvokeAllAsync(TRequest request, CancellationToken cancellationToken = default);
    ValueTask<TResponse[]> InvokeAllAsync(TRequest request, AsyncPublishStrategy publishStrategy, CancellationToken cancellationToken = default);
    // snip others...
}
```

`MessagePipeOptions.DefaultAsyncPublishStrategy`'s default is `Parallel`.

### HandlingSubscribeDisposedPolicy

When `ISubscriber.Subscribe` after MessageBroker(publisher/subscriber manager) is disposed(for example, scope is disposed), choose `Ignore`(returns empty `IDisposable`) or `Throw` exception. Default is `Ignore`.

### InstanceLifetime

Configure MessageBroker(publisher/subscriber manager)'s lifetime of DI cotainer. You can choose `Singleton` or `Scoped`. Default is `Singleton`. When choose `Scoped`, each messagebrokers manage different subscribers and when scope is disposed, unsubscribe all managing subscribers.

### RequestHandlerLifetime

Configure IRequestHandler/IAsyncRequestHandler's lifetime of DI container. You can choose `Singleton` or `Scoped` or `Transient`. Default is `Scoped`.

### EnableAutoRegistration/SetAutoRegistrationSearchAssemblies/SetAutoRegistrationSearchTypes

Register `IRequestHandler`, `IAsyncHandler` and filters to DI container automatically on startup. Default is `true` and default search target is CurrentDomain's all assemblies and types. However, this sometimes fails to detect the assembly being stripped. In that case, you can enable the search by explicitly adding it to `SetAutoRegistrationSearchAssemblies` or `SetAutoRegistrationSearchTypes`.

`[IgnoreAutoRegistration]` attribute allows to disable auto registration which attribute attached.

### EnableCaptureStackTrace

See the details [Managing Subscription and Diagnostics](#managing-subscription-and-diagnostics) section, if `true` then capture stacktrace on Subscribe. It is useful for debugging but performance will be degraded. Default is `false` and recommended to enable only debug.

### AddGlobal***Filter

Add global filter, for example logging filter will be useful.

```csharp
public class LoggingFilter<T> : MessageHandlerFilter<T>
{
    readonly ILogger<LoggingFilter<T>> logger;

    public LoggingFilter(ILogger<LoggingFilter<T>> logger)
    {
        this.logger = logger;
    }

    public override void Handle(T message, Action<T> next)
    {
        try
        {
            logger.LogDebug("before invoke.");
            next(message);
            logger.LogDebug("invoke completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "error");
        }
    }
}
```

To enable all types, use open generics.

```csharp
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services.AddMessagePipe(options =>
        {
            // use typeof(Filter<>, order);
            options.AddGlobalMessageHandlerFilter(typeof(LoggingFilter<>), -10000);
        });
    });
```

Global provider
---
If you want to get publisher/subscriber/handler from globally scope, get `IServiceProvider` before run and set to static helper called `GlobalMessagePipe`.

```csharp
var host = Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, x) =>
    {
        x.AddMessagePipe();
    })
    .Build(); // build host before run.

GlobalMessagePipe.SetProvider(host.Services); // set service provider

await host.RunAsync(); // run framework.
```

`GlobalMessagePipe` has these static method(`GetPublisher<T>`, `GetSubscriber<T>`, `CreateEvent<T>`, etc...) so you can get globally.

![image](https://user-images.githubusercontent.com/46207/116521078-7c00de00-a90e-11eb-85c0-2c62c140c51d.png)

Integration with other DI library
---
All(popular) DI libraries has `Microsoft.Extensions.DependencyInjection` bridge so configure by MS.E.DI and use bridge if you want.

Compare with Channels
---
[System.Threading.Channels](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels)(for Unity, `UniTask.Channels`) uses Queue internal, the producer is not affected by the performance of the consumer, and the consumer can control the flow rate(back pressure). This is a different use than MessagePipe's Pub/Sub.

Unity
---
You need to install Core library and choose [VContainer](https://github.com/hadashiA/VContainer/) or [Zenject](https://github.com/modesttree/Zenject) or `BuiltinContainerBuilder` for runtime. You can install via UPM git URL package or asset package(MessagePipe.*.unitypackage) available in [MessagePipe/releases](https://github.com/Cysharp/MessagePipe/releases) page.

* Core `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe`
* VContainer `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.VContainer`
* Zenject `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.Zenject`

Andalso, requires [UniTask](https://github.com/Cysharp/UniTask) to install, all `ValueTask` declaration in .NET is replaced to `UniTask`.

> [!NOTE]
> Unity version does not have open generics support(for IL2CPP) and does not support auto registration. Therefore, all required types need to be manually registered.

VContainer's installation sample.

```csharp
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // RegisterMessagePipe returns options.
        var options = builder.RegisterMessagePipe(/* configure option */);
        
        // Setup GlobalMessagePipe to enable diagnostics window and global function
        builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));

        // RegisterMessageBroker: Register for IPublisher<T>/ISubscriber<T>, includes async and buffered.
        builder.RegisterMessageBroker<int>(options);

        // also exists RegisterMessageBroker<TKey, TMessage>, RegisterRequestHandler, RegisterAsyncRequestHandler

        // RegisterMessageHandlerFilter: Register for filter, also exists RegisterAsyncMessageHandlerFilter, Register(Async)RequestHandlerFilter
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

> [!TIP]
> If you are using Unity 2022.1 or later and VContainer 1.14.0 or later, you do not need `RegsiterMessageBroker<>`. 
> A set of types including `ISubscriber<>`, `IPublisher<>` or its asynchronous version will be resolved automatically.
> Note that `IRequesthandler<>` and `IRequestAllHanlder<>` still require manual registration.


Unity version does not have open generics support(for IL2CPP) and does not support auto registration. Therefore, all required types need to be manually registered.


Zenject's installation sample.

```csharp
void Configure(DiContainer builder)
{
    // BindMessagePipe returns options.
    var options = builder.BindMessagePipe(/* configure option */);
    
    // BindMessageBroker: Register for IPublisher<T>/ISubscriber<T>, includes async and buffered.
    builder.BindMessageBroker<int>(options);

    // also exists BindMessageBroker<TKey, TMessage>, BindRequestHandler, BindAsyncRequestHandler

    // BindMessageHandlerFilter: Bind for filter, also exists BindAsyncMessageHandlerFilter, Bind(Async)RequestHandlerFilter
    builder.BindMessageHandlerFilter<MyFilter<int>>();

    // set global to enable diagnostics window and global function
    GlobalMessagePipe.SetProvider(builder.AsServiceProvider());
}
```

> Zenject version is not supported `InstanceScope.Singleton` for Zenject's limitation. The default is `Scoped`, which cannot be changed.

`BuiltinContainerBuilder` is builtin minimum DI library for MessagePipe, it no needs other DI library to use MessagePipe. Here is installation sample.

```csharp
var builder = new BuiltinContainerBuilder();

builder.AddMessagePipe(/* configure option */);

// AddMessageBroker: Register for IPublisher<T>/ISubscriber<T>, includes async and buffered.
builder.AddMessageBroker<int>(options);

// also exists AddMessageBroker<TKey, TMessage>, AddRequestHandler, AddAsyncRequestHandler

// AddMessageHandlerFilter: Register for filter, also exists RegisterAsyncMessageHandlerFilter, Register(Async)RequestHandlerFilter
builder.AddMessageHandlerFilter<MyFilter<int>>();

// create provider and set to Global(to enable diagnostics window and global fucntion)
var provider = builder.BuildServiceProvider();
GlobalMessagePipe.SetProvider(provider);

// --- to use MessagePipe, you can use from GlobalMessagePipe.
var p = GlobalMessagePipe.GetPublisher<int>();
var s = GlobalMessagePipe.GetSubscriber<int>();

var d = s.Subscribe(x => Debug.Log(x));

p.Publish(10);
p.Publish(20);
p.Publish(30);

d.Dispose();
```

> BuiltinContainerBuilder does not supports scope(always `InstanceScope.Singleton`), `IRequestAllHandler/IAsyncRequestAllHandler`, and many DI functionally, so we recommend to use by `GlobalMessagePipe` when use BuiltinContainerBuilder.

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

Also you can use `GlobalMessagePipe` and `MessagePipe Diagnostics` window. see: [Global provider](#global-provider) and [Managing Subscription and Diagnostics](#managing-subscription-and-diagnostics) section.

License
---
This library is licensed under the MIT License.

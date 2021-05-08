using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;
using System;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        var options = builder.RegisterMessagePipe(x => { x.EnableCaptureStackTrace = true; });
        builder.RegisterMessageBroker<int>(options);


        builder.RegisterEntryPoint<MessagePipeDemo>(Lifetime.Singleton);
    }

    // Register IPublisher<T>/ISubscriber<T> and global filter.
    static void RegisterMessageBroker<T>(IContainerBuilder builder, MessagePipeOptions options)
    {
        builder.RegisterMessageBroker<T>(options);

        // setup for global filters.
        options.AddGlobalMessageHandlerFilter<MyMessageHandlerFilter<T>>();
    }

    static void RegisterRequest<TRequest, TResponse, THandler>(IContainerBuilder builder, MessagePipeOptions options)
        where THandler : IRequestHandler
    {
        builder.RegisterRequestHandler<TRequest, TResponse, THandler>(options);
        options.AddGlobalRequestHandlerFilter<MyRequestHandlerFilter<TRequest, TResponse>>();
    }
}

public class MyMessageHandlerFilter<T> : MessageHandlerFilter<T>
{
    public override void Handle(T message, System.Action<T> next)
    {
        throw new System.NotImplementedException();
    }
}

public class MyRequestHandlerFilter<TReq, TRes> : RequestHandlerFilter<TReq, TRes>
{
    public override TRes Invoke(TReq request, System.Func<TReq, TRes> next)
    {
        throw new System.NotImplementedException();
    }
}

public class MessagePipeDemo : VContainer.Unity.IStartable
{
    readonly IPublisher<int> publisher;
    readonly ISubscriber<int> subscriber;

    public MessagePipeDemo(IPublisher<int> publisher, ISubscriber<int> subscriber, IObjectResolver resolver)
    {
        this.publisher = publisher;
        this.subscriber = subscriber;

        GlobalMessagePipe.SetProvider(resolver.AsServiceProvider());
    }

    public void Start()
    {



        var d = DisposableBag.CreateBuilder();

        // subscriber.Subscribe(x => Debug.Log("P1:" + x));





        subscriber.Subscribe(x => Debug.Log("P1:" + x)).AddTo(d);
        subscriber.Subscribe(x => Debug.Log("P2:" + x)).AddTo(d);

        publisher.Publish(10);
        publisher.Publish(20);
        publisher.Publish(30);

        var disposable = d.Build();
        // disposable.Dispose();
    }
}











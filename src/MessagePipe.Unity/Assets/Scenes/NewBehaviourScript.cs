using Cysharp.Threading.Tasks;
using MessagePipe;
using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class NewBehaviourScript : MonoBehaviour
{
    public Button button1;
    public Button button2;
    public Button button3;
    public Button button4;

    IPublisher<int> publisher;
    ISubscriber<int> subscriber;
    IDisposable disposable;
    int id = 0;

    public void Start()
    {
        var builder = new ContainerBuilder();

        var options = builder.RegisterMessagePipe(x => { x.EnableCaptureStackTrace = true; });
        builder.RegisterMessageBroker<int>(options);

        var resolver = builder.Build();
        GlobalMessagePipe.SetProvider(resolver.AsServiceProvider());

        publisher = resolver.Resolve<IPublisher<int>>();
        subscriber = resolver.Resolve<ISubscriber<int>>();

        button1.onClick.AddListener(FooSubscribe);
        button2.onClick.AddListener(BarSubscribe);
        button3.onClick.AddListener(() =>
        {
            disposable = DisposableBag.Create(disposable, subscriber.Subscribe(x => Debug.Log($"{id}:Baz")));
        });
        button4.onClick.AddListener(UnSubscribeAll);

        disposable = DisposableBag.Empty;
        Forever().Forget();
    }

    void FooSubscribe()
    {
        disposable = DisposableBag.Create(disposable, subscriber.Subscribe(x => Debug.Log($"{id}:Foo1")));
        disposable = DisposableBag.Create(disposable, subscriber.Subscribe(x => Debug.Log($"{id}:Foo2")));
    }

    void BarSubscribe()
    {
        disposable = DisposableBag.Create(disposable, subscriber.Subscribe(x => Debug.Log($"{id}:Bar")));
    }

    //void BazSubscribe()
    //{
    //    disposable = DisposableBag.Create(disposable, subscriber.Subscribe(x => Debug.Log($"{id}:Baz")));
    //}

    void UnSubscribeAll()
    {
        disposable.Dispose();
        disposable = DisposableBag.Empty;
    }

    async UniTaskVoid Forever()
    {
        var cts = this.GetCancellationTokenOnDestroy();
        while (!cts.IsCancellationRequested)
        {
            await UniTask.Yield();
        }
    }
}
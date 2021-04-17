using Easy.MessageHub;
using GalaSoft.MvvmLight.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using PubSub;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zenject;

namespace MessagePipe.Benchmark
{
    public class PublishOps
    {
        IPublisher<Message> p;
        Message m;
        Subject<Message> subject;
        PubSubEvent<Message> prism;
        PubSubEvent<Message> prismStrong;
        IMediator medi;
        public event Action<Message> ev;
        Hub hub;
        SignalBus signalBus;

        Messenger mvvmLight;
        Messenger mvvmLightStrong;
        IPublisher<Message> filter1;
        IPublisher<Message> filter2;
        IPublisher<Guid, Message> keyed;
        Guid key = Guid.NewGuid();

        MessageHub easyMsgHub;

        public PublishOps()
        {
            var provider = new ServiceCollection().AddMessagePipe().BuildServiceProvider();

            prism = new Prism.Events.EventAggregator().GetEvent<Message>();
            prismStrong = new Prism.Events.EventAggregator().GetEvent<Message>();

            var mdiatr = new ServiceCollection().AddMediatR(typeof(PublishOps).Assembly).BuildServiceProvider();
            medi = mdiatr.GetRequiredService<IMediator>();


            p = provider.GetRequiredService<IPublisher<Message>>();
            var s = provider.GetRequiredService<ISubscriber<Message>>();

            keyed = provider.GetRequiredService<IPublisher<Guid, Message>>();
            var keyedS = provider.GetRequiredService<ISubscriber<Guid, Message>>();

            hub = Hub.Default;



            var px = new ServiceCollection().AddMessagePipe().BuildServiceProvider();
            filter1 = px.GetRequiredService<IPublisher<Message>>();
            var filter1Sub = px.GetRequiredService<ISubscriber<Message>>();

            var px2 = new ServiceCollection().AddMessagePipe().BuildServiceProvider();
            filter2 = px2.GetRequiredService<IPublisher<Message>>();
            var filter2Sub = px2.GetRequiredService<ISubscriber<Message>>();

            m = new Message();
            subject = new Subject<Message>();

            signalBus = SetupZenject();

            mvvmLight = new Messenger();
            mvvmLightStrong = new Messenger();


             easyMsgHub = new MessageHub();




            for (int i = 0; i < 8; i++)
            {
                s.Subscribe(new EmptyMessageHandler());
                prism.Subscribe(_ => { });
                prismStrong.Subscribe(_ => { }, true);
                ev += _ => { };
                subject.Subscribe(_ => { });
                hub.Subscribe<Message>(_ => { });
                UniRx.MessageBroker.Default.Receive<Message>().Subscribe(new NopObserver());
                mvvmLight.Register<Message>(this, _ => { }, false);
                // mvvmLightStrong.Register<Message>(this, _ => { }, true);

                keyedS.Subscribe(key, _ => { });

                filter1Sub.Subscribe(new EmptyMessageHandler(), new EmptyMessageHandlerFilter());
                filter2Sub.Subscribe(new EmptyMessageHandler(), new EmptyMessageHandlerFilter(), new EmptyMessageHandlerFilter());

                easyMsgHub.Subscribe<Message>(_ => { });
            }

            signalBus.Subscribe<Message>(m => { });
            signalBus.Subscribe<Message>(m => { });
            signalBus.Subscribe<Message>(m => { });
            signalBus.Subscribe<Message>(m => { });
            signalBus.Subscribe<Message>(m => { });
            signalBus.Subscribe<Message>(m => { });
            signalBus.Subscribe<Message>(m => { });
            signalBus.Subscribe<Message>(m => { });


        }

        SignalBus SetupZenject()
        {
            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<Message>();
            return container.Resolve<SignalBus>();
        }

        public async Task MeasureAllAsync()
        {
            (string, int)[] result = new (string, int)[0];
            for (int i = 0; i < 2; i++)
            {
                if (i == 0) Console.WriteLine("WARM:");
                if (i == 1) Console.WriteLine("RUN:");

                result = new (string, int)[]
                {
                    Measure("Cysharp.NewLib(WIP)", () => p.Publish(m)),
                    Measure("event", () => ev(m)),
                    Measure("Rx.Subject", () => subject.OnNext(m)),
                    Measure("Prism", () => prism.Publish(m)),
                    Measure("Prism(keepRef)", () => prismStrong.Publish(m)),
                    await MeasureAsync("MediatR", () => medi.Publish(m)),
                    Measure("upta/PubSub", () => hub.Publish(m)),
                    Measure("UniRx.MessageBroker", () => UniRx.MessageBroker.Default.Publish(m)),
                    Measure("Zenject.Signals", () => signalBus.Fire<Message>(m)),
                    Measure("MvvmLight", () => mvvmLight.Send(m)),
                    // Measure("MvvmLight(keepRef)", () => mvvmLightStrong.Send(m))
                    Measure("Easy.MessageHub", () => easyMsgHub.Publish(m)),
                


                    //Measure("MessagePipe(f1)", () => filter1.Publish(m)),
                    //Measure("MessagePipe(f2)", () => filter2.Publish(m)),
                    //Measure("MessagePipe(key)", () => keyed.Publish(key, m)),
                };
            }

            Console.WriteLine("----");
            Console.WriteLine();

            foreach (var item in result.OrderByDescending(x => x.Item2))
            {
                Console.WriteLine(string.Format("  {0,-20} {1,10} op/sec", item.Item1, item.Item2));
            }

            Console.WriteLine();
        }

        static (string, int) Measure(string label, Action action)
        {
            Console.WriteLine("Start:" + label);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            GC.TryStartNoGCRegion(1000 * 1000 * 100, true);

            var count = 0;
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= 1000)
            {
                action();
                count++;
            }

            try
            {
                GC.EndNoGCRegion();
            }
            catch
            {
                Console.WriteLine("Faile NoGC:" + label);
            }

            return (label, count);
        }

        static async Task<(string, int)> MeasureAsync(string label, Func<Task> action)
        {
            Console.WriteLine("Start:" + label);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            GC.TryStartNoGCRegion(1000 * 1000, true);

            var count = 0;
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds <= 1000)
            {
                await action().ConfigureAwait(false);
                count++;
            }

            try
            {
                GC.EndNoGCRegion();
            }
            catch
            {
                Console.WriteLine("Faile NoGC:" + label);
            }

            return (label, count);
        }


    }


    public class Message : PubSubEvent<Message>, INotification
    {
    }

    public class NopObserver : UniRx.IObserver<Message>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(Message value)
        {
        }
    }

    public class Pong0 : INotificationHandler<Message>
    {
        public Task Handle(Message notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    public class Pong1 : INotificationHandler<Message>
    {
        public Task Handle(Message notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    public class Pong2 : INotificationHandler<Message>
    {
        public Task Handle(Message notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    public class Pong3 : INotificationHandler<Message>
    {
        public Task Handle(Message notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    public class Pong4 : INotificationHandler<Message>
    {
        public Task Handle(Message notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    public class Pong5 : INotificationHandler<Message>
    {
        public Task Handle(Message notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    public class Pong6 : INotificationHandler<Message>
    {
        public Task Handle(Message notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    public class Pong7 : INotificationHandler<Message>
    {
        public Task Handle(Message notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class EmptyMessageHandler : IMessageHandler<Message>
    {
        public void Handle(Message message)
        {
        }
    }

    public class EmptyMessageHandlerFilter : MessageHandlerFilter
    {
        public override void Handle<T>(T message, Action<T> next)
        {
            next(message);
        }
    }
}

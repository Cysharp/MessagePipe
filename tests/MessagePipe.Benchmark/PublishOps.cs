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
        IPublisher<Message> p2;
        Message m;
        Subject<Message> subject;
        PubSubEvent<Message> prism;
        PubSubEvent<Message> prismStrong;
        IMediator medi;
        ImmutableArrayMessageBroker<Message> direct;
        public event Action<Message> ev;
        Hub hub;
        SignalBus signalBus;

        Messenger mvvmLight;
        Messenger mvvmLightStrong;

        public PublishOps()
        {
            var provider = new ServiceCollection().AddMessagePipe().BuildServiceProvider();
            var provider2 = new ServiceCollection().AddMessagePipe(x => { x.DefaultHandlerRepository = DefaultHandlerRepository.ConcurrentDictionary; }).BuildServiceProvider();

            prism = new Prism.Events.EventAggregator().GetEvent<Message>();
            prismStrong = new Prism.Events.EventAggregator().GetEvent<Message>();

            var mdiatr = new ServiceCollection().AddMediatR(typeof(PublishOps).Assembly).BuildServiceProvider();
            medi = mdiatr.GetRequiredService<IMediator>();

            direct = new ImmutableArrayMessageBroker<Message>(provider.GetRequiredService<MessagePipeDiagnosticsInfo>());

            p = provider.GetRequiredService<IPublisher<Message>>();
            p2 = provider2.GetRequiredService<IPublisher<Message>>();
            var s = provider.GetRequiredService<ISubscriber<Message>>();
            var s2 = provider2.GetRequiredService<ISubscriber<Message>>();
            hub = Hub.Default;

            m = new Message();
            subject = new Subject<Message>();

            signalBus = SetupZenject();

            mvvmLight = new Messenger();
            mvvmLightStrong = new Messenger();

            for (int i = 0; i < 8; i++)
            {
                s.Subscribe(new EmptyMessageHandler());
                s2.Subscribe(new EmptyMessageHandler());
                direct.Subscribe(new EmptyMessageHandler());
                prism.Subscribe(_ => { });
                prismStrong.Subscribe(_ => { }, true);
                ev += _ => { };
                subject.Subscribe(_ => { });
                hub.Subscribe<Message>(_ => { });
                UniRx.MessageBroker.Default.Receive<Message>().Subscribe(new NopObserver());
                mvvmLight.Register<Message>(this, _ => { }, false);
                mvvmLight.Register<Message>(this, _ => { }, true);
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
            var result = new (string, int)[]
            {
                Measure("MessagePipe", () => p.Publish(m)),
                Measure("MessagePipe(Slow)", () => p2.Publish(m)),
                Measure("event", () => ev(m)),
                Measure("Rx.Subject", () => subject.OnNext(m)),
                Measure("Prism", () => prism.Publish(m)),
                Measure("Prism(keepRef )", () => prismStrong.Publish(m)),
                await MeasureAsync("MediatR", () => medi.Publish(m)),
                Measure("MessagePipe(Direct)", () => direct.Publish(m)),
                Measure("upta/PubSub", () => hub.Publish(m)),
                Measure("UniRx.MessageBroker", () => UniRx.MessageBroker.Default.Publish(m)),
                Measure("Zenject.Signals", () => signalBus.Fire<Message>(m)),
                Measure("MvvmLight", () => mvvmLight.Send(m)),
                Measure("MvvmLight(keepRef)", () => mvvmLight.Send(m))
            };

            Console.WriteLine("----");

            foreach (var item in result.OrderByDescending(x => x.Item2))
            {
                Console.WriteLine(string.Format("{0,-20} {1,10} op/sec", item.Item1, item.Item2));
            }
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
}

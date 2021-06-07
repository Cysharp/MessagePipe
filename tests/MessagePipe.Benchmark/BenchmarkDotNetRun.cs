#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using BenchmarkDotNet.Attributes;
using Easy.MessageHub;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using PubSub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace MessagePipe.Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class BenchmarkDotNetRun
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

        IPublisher<Guid, Message> keyed;
        Guid key = Guid.NewGuid();

        MessageHub easyMsgHub;

        public BenchmarkDotNetRun()
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

            m = new Message();
            subject = new Subject<Message>();

            signalBus = SetupZenject();
            easyMsgHub = new MessageHub();

            for (int i = 0; i < 8; i++)
            {
                s.Subscribe(new EmptyMessageHandler());
                prism.Subscribe(_ => { });
                prismStrong.Subscribe(_ => { }, true);
                ev += _ => { };
                subject.Subscribe(_ => { });
                hub.Subscribe<Message>(_ => { });



                keyedS.Subscribe(key, _ => { });


                easyMsgHub.Subscribe<Message>(_ => { });


            }

            signalBus.Subscribe<Message>(_ => { });
            signalBus.Subscribe<Message>(_ => { });
            signalBus.Subscribe<Message>(_ => { });
            signalBus.Subscribe<Message>(_ => { });
            signalBus.Subscribe<Message>(_ => { });
            signalBus.Subscribe<Message>(_ => { });
            signalBus.Subscribe<Message>(_ => { });
            signalBus.Subscribe<Message>(_ => { });

        }

        SignalBus SetupZenject()
        {
            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<Message>();
            return container.Resolve<SignalBus>();
        }

        [Benchmark]
        public void MessagePipe()
        {
            p.Publish(m);
        }

        [Benchmark]
        public void Event()
        {
            ev(m);
        }

        [Benchmark]
        public void RxSubject()
        {
            subject.OnNext(m);
        }

        [Benchmark]
        public void Prism()
        {
            prism.Publish(m);
        }

        [Benchmark]
        public void Prism_KeepRef()
        {
            prismStrong.Publish(m);
        }

        [Benchmark]
        public void MediatR()
        {
            medi.Publish(m).Wait();
        }

        [Benchmark]
        public void UptaPubSub()
        {
            hub.Publish(m);
        }

        [Benchmark]
        public void ZenjectSignals()
        {
            signalBus.Fire<Message>(m);
        }

        public void EasyMessageHub()
        {
            easyMsgHub.Publish(m);
        }
    }
}

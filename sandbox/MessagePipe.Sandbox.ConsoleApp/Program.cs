using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZLogger;

namespace MessagePipe
{
    class Program : ConsoleAppBase
    {
        static async Task Main(string[] args)
        {
            args = new[] { "predicate" };

            await Host.CreateDefaultBuilder()
                .ConfigureServices(x =>
                {
                    x.AddMessagePipe();

                    // todo:automatically register it.
                    x.AddTransient(typeof(IRequestHandler<Ping, Pong>), typeof(PingHandler));
                    x.AddTransient(typeof(IRequestHandler<Ping, Pong>), typeof(PingHandler2));
                    x.AddTransient(typeof(PingHandler));
                    x.AddSingleton(typeof(MyFilter));

                })
                .ConfigureLogging(x =>
                {
                    x.ClearProviders();
                    x.SetMinimumLevel(LogLevel.Information);
                    x.AddZLoggerConsole();
                })
                .RunConsoleAppFrameworkAsync<Program>(args);
        }

        IPublisher<string, MyMessage> publisher;
        ISubscriber<string, MyMessage> subscriber;
        IPublisher<MyMessage> keylessP;
        ISubscriber<MyMessage> keylessS;
        IAsyncPublisher<MyMessage> asyncKeylessP;
        IAsyncSubscriber<MyMessage> asyncKeylessS;


        IRequestHandler<Ping, Pong> pingponghandler;
        IRequestAllHandler<Ping, Pong> pingallhandler;
        PingHandler pingpingHandler;


        IPublisher<int> intPublisher;
        ISubscriber<int> intSubscriber;

        public Program(IPublisher<string, MyMessage> publisher, ISubscriber<string, MyMessage> subscriber,
            IPublisher<MyMessage> keyless1,
            ISubscriber<MyMessage> keyless2,

            IAsyncPublisher<MyMessage> asyncKeylessP,
            IAsyncSubscriber<MyMessage> asyncKeylessS,

            IRequestHandler<Ping, Pong> pingponghandler,
            PingHandler pingpingHandler,
            IRequestAllHandler<Ping, Pong> pingallhandler,

            IPublisher<int> intP,
            ISubscriber<int> intS
            )
        {
            this.publisher = publisher;
            this.subscriber = subscriber;
            this.keylessP = keyless1;
            this.keylessS = keyless2;
            this.asyncKeylessP = asyncKeylessP;
            this.asyncKeylessS = asyncKeylessS;
            this.pingponghandler = pingponghandler;
            this.pingpingHandler = pingpingHandler;
            this.pingallhandler = pingallhandler;
            this.intPublisher = intP;
            this.intSubscriber = intS;
        }

        [Command("keyed")]
        public void Keyed()
        {
            this.subscriber.Subscribe("foo", x =>
            {
                Console.WriteLine("A:" + x.MyProperty);
            });

            var d = this.subscriber.Subscribe("foo", x =>
             {
                 Console.WriteLine("B:" + x.MyProperty);
             });

            publisher.Publish("foo", new MyMessage() { MyProperty = "tako" });
            publisher.Publish("foo", new MyMessage() { MyProperty = "yaki" });

            d.Dispose();

            publisher.Publish("foo", new MyMessage() { MyProperty = "kamo" });
        }

        [Command("keyless")]
        public void Keyless()
        {
            this.keylessS.Subscribe(x =>
            {
                Console.WriteLine("A:" + x.MyProperty);
            });

            var d = this.keylessS.Subscribe(x =>
            {
                Console.WriteLine("B:" + x.MyProperty);
            });


            keylessP.Publish(new MyMessage() { MyProperty = "tako" });
            keylessP.Publish(new MyMessage() { MyProperty = "yaki" });



            d.Dispose();

            keylessP.Publish(new MyMessage() { MyProperty = "kamo" });
        }

        [Command("asynckeyless")]
        public async Task AsyncKeyless()
        {
            this.asyncKeylessS.Subscribe(async (x, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
                Console.WriteLine("A:" + x.MyProperty);
            });

            var d = this.asyncKeylessS.Subscribe(async (x, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                Console.WriteLine("B:" + x.MyProperty);
            });

            await asyncKeylessP.PublishAsync(new MyMessage() { MyProperty = "tako" });
            await asyncKeylessP.PublishAsync(new MyMessage() { MyProperty = "yaki" });

            Console.WriteLine("here?");

            d.Dispose();

            await asyncKeylessP.PublishAsync(new MyMessage() { MyProperty = "kamo" });
        }

        [Command("ping")]
        public void Ping()
        {
            Console.WriteLine("ping");
            var pong = pingponghandler.Execute(new Ping());
            Console.WriteLine("pong");
        }

        [Command("pingmany")]
        public void PingMany()
        {
            Console.WriteLine("ping");
            var pong = pingallhandler.ExecuteAll(new Ping());
            foreach (var item in pong)
            {
                Console.WriteLine("pong");
            }
        }

        event Action myEventAction;

        [Command("myevent")]
        public void MyEvent()
        {
            myEventAction += () => Console.WriteLine("ev one");
            myEventAction += () => Console.WriteLine("ev two");
            myEventAction();

            myEventAction += () =>
            {
                Console.WriteLine("eve three and exception");
                throw new Exception("???");
            };

            myEventAction += () => Console.WriteLine("ev four");
            myEventAction();
        }

        [Command("mydelegate")]
        public void MyDelegate()
        {
            var d1 = new FooMore().GetDelegate();
            var d2 = new BarMore().GetDelegate();
        }

        [Command("filter")]
        public void Filter()
        {
            this.keylessS.Subscribe(new MyFirst());

            keylessP.Publish(new MyMessage() { MyProperty = "tako" });
            keylessP.Publish(new MyMessage() { MyProperty = "yaki" });
        }

        [Command("predicate")]
        public void Pred()
        {
            this.keylessS.Subscribe(x =>
            {
                Console.WriteLine("FilteredA:" + x.MyProperty);
            }, x => x.MyProperty == "foo" || x.MyProperty == "hoge");


            this.keylessS.Subscribe(x =>
            {
                Console.WriteLine("FilteredB:" + x.MyProperty);
            }, x => x.MyProperty == "foo" || x.MyProperty == "hage");

            this.keylessP.Publish(new MyMessage { MyProperty = "nano" });
            this.keylessP.Publish(new MyMessage { MyProperty = "foo" });
            this.keylessP.Publish(new MyMessage { MyProperty = "hage" });
            this.keylessP.Publish(new MyMessage { MyProperty = "hoge" });

            this.intSubscriber.Subscribe(x => Console.WriteLine(x), x => x < 10);
            this.intPublisher.Publish(999);
            this.intPublisher.Publish(5);
        }
    }

    public class PingHandler : IRequestHandler<Ping, Pong>
    {
        public Pong Execute(Ping request)
        {
            Console.WriteLine("1 ping");
            return new Pong();
        }
    }

    public class PingHandler2 : IRequestHandler<Ping, Pong>
    {
        public Pong Execute(Ping request)
        {
            Console.WriteLine("2 ping");
            return new Pong();
        }
    }


    public class MyClass
    {

    }


    public class MyFilter : MessageHandlerFilter
    {
        public override void Handle<T>(T message, Action<T> next)
        {
            Console.WriteLine("before");
            next(message);
            Console.WriteLine("after");
        }
    }

    [MessageHandlerFilter(typeof(MyFilter))]
    [MessageHandlerFilter(typeof(MyFilter))]
    [MessageHandlerFilter(typeof(MyFilter))]
    public class MyFirst : IMessageHandler<MyMessage>
    {
        public void Handle(MyMessage message)
        {
            Console.WriteLine("YEAHHHH:" + message.MyProperty);
        }
    }


    public class Ping
    {
    }

    public class Pong
    {
    }

    public class FooMore
    {
        public int Tako;
        public int Nano;

        public Action GetDelegate() => Ahokkusu;

        public void Ahokkusu()
        {
            Console.WriteLine("nano");
        }
    }

    public struct BarMore
    {
        public int Tako;
        public int Nano;

        public Action GetDelegate() => Ahokkusu;

        public void Ahokkusu()
        {
            Console.WriteLine("nano");
        }
    }

    public class MyMessage
    {
        public string MyProperty { get; set; }
    }

    // .UseDistributedAsyncPublisher();


    // DistributedAsyncPublisher




    public class KeyedMessageBrokerCore<TKey, TValue>
    {

    }

}

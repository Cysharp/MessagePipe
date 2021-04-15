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
            next(message);
        }
    }


    public class Ping
    {
    }

    public class Pong
    {
    }

    class Program : ConsoleAppBase
    {
        static async Task Main(string[] args)
        {
            args = new[] { "mydelegate" };

            await Host.CreateDefaultBuilder()
                .ConfigureServices(x =>
                {
                    // keyed PubSub
                    x.AddSingleton(typeof(IMessageBroker<,>), typeof(ImmutableArrayMessageBroker<,>));
                    x.AddTransient(typeof(IPublisher<,>), typeof(MessageBroker<,>));
                    x.AddTransient(typeof(ISubscriber<,>), typeof(MessageBroker<,>));

                    // keyless PubSub
                    x.AddSingleton(typeof(IMessageBroker<>), typeof(ImmutableArrayMessageBroker<>));
                    x.AddTransient(typeof(IPublisher<>), typeof(MessageBroker<>));
                    x.AddTransient(typeof(ISubscriber<>), typeof(MessageBroker<>));

                    // keyless PubSub async
                    x.AddSingleton(typeof(IAsyncMessageBroker<>), typeof(ImmutableArrayAsyncMessageBroker<>));
                    x.AddTransient(typeof(IAsyncPublisher<>), typeof(AsyncMessageBroker<>));
                    x.AddTransient(typeof(IAsyncSubscriber<>), typeof(AsyncMessageBroker<>));

                    // manual?
                    // todo:automatically register it.
                    x.AddTransient(typeof(IRequestHandler<Ping, Pong>), typeof(PingHandler));
                    x.AddTransient(typeof(IRequestHandler<Ping, Pong>), typeof(PingHandler2));
                    x.AddTransient(typeof(PingHandler));

                    // RequestAll
                    x.AddTransient(typeof(IRequestAllHandler<,>), typeof(RequestAllHandler<,>));

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

        public Program(IPublisher<string, MyMessage> publisher, ISubscriber<string, MyMessage> subscriber,
            IPublisher<MyMessage> keyless1,
            ISubscriber<MyMessage> keyless2,

            IAsyncPublisher<MyMessage> asyncKeylessP,
            IAsyncSubscriber<MyMessage> asyncKeylessS,

            IRequestHandler<Ping, Pong> pingponghandler,
            PingHandler pingpingHandler,
            IRequestAllHandler<Ping, Pong> pingallhandler
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
        }

        [Command("keyed")]
        public void Keyed()
        {
            this.subscriber.Subscribe("foo", x =>
            {
                Console.WriteLine("A:" + x.MyProperty);
            });

            [MessagePipeFilter(typeof(MyFilter))]
            static void Foo(MyMessage msg)
            {
                Console.WriteLine("Yeah");
            }
            this.subscriber.Subscribe("foo", Foo);

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

    public enum AsyncPublishStrategy
    {
        Sequential,
        Parallel
    }



    public class KeyedMessageBrokerCore<TKey, TValue>
    {

    }

}

#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using ConsoleAppFramework;
using MessagePipe.Sandbox.ConsoleApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZLogger;

namespace MessagePipe
{
    public class MyOption
    {
        public int MyProperty { get; set; }
    }

    [IgnoreAutoRegistration]
    public class MyGenericsHandler<TR2> : IRequestHandler<string, TR2>
    {
        public TR2 Invoke(string request)
        {
            Console.WriteLine("everything default!");
            return default(TR2);
        }
    }
    //[IgnoreAutoRegistration]
    public class MyMyGenericsHandler<TR1, TR2> : IRequestHandler<TR1, TR2>
    {
        public TR2 Invoke(TR1 request)
        {
            Console.WriteLine("everything default!");
            return default(TR2);
        }
    }

    [IgnoreAutoRegistration]
    public class MyGenericsHandler2 : IRequestHandler<int, int>
    {
        public int Invoke(int request)
        {
            Console.WriteLine("everything default 2!");
            return default(int);
        }
    }

    class Program : ConsoleAppBase
    {
        static async Task Main3(string[] args)
        {
            var c = new ServiceCollection();
            var p = c.BuildServiceProvider();
            var p2 = p.GetRequiredService<IServiceProvider>();


            var host = Host.CreateDefaultBuilder()
                    .ConfigureServices((_, x) =>
                    {

                        x.AddMessagePipe();
                    })
                    .Build(); // build host before run.

            GlobalMessagePipe.SetProvider(host.Services); // set service provider

            await host.RunAsync(); // run framework.



            args = new[] { "moremore" };

            await Host.CreateDefaultBuilder()
                .ConfigureServices((_, x) =>
                {


                    x.AddMessagePipe(options =>
                    {
                        options.InstanceLifetime = InstanceLifetime.Singleton;
                        options.EnableCaptureStackTrace = true;

                        options.AddGlobalMessageHandlerFilter(typeof(MyFilter<>));
                        //options.AddGlobalMessageHandlerFilter<MyFilter<MyMessage>>();
                    });


                    //var interfaceType = typeof(IRequestHandlerCore<,>);
                    //var objectType = typeof(MyGenericsHandler<,>);
                    //x.Add(interfaceType, objectType, InstanceLifetime.Singleton);

                    //x.AddRequestHandler(typeof(MyMyGenericsHandler<,> ));
                    //x.AddRequestHandler(typeof(MyGenericsHandler<>));


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
        // PingHandler pingpingHandler;


        IPublisher<int> intPublisher;
        ISubscriber<int> intSubscriber;

        IServiceScopeFactory scopeF;
        MessagePipeDiagnosticsInfo diagnosticsInfo;

        IServiceProvider provider;

        public Program(
            IPublisher<string, MyMessage> publisher,
            ISubscriber<string, MyMessage> subscriber,
            IPublisher<MyMessage> keyless1,
            ISubscriber<MyMessage> keyless2,

            IAsyncPublisher<MyMessage> asyncKeylessP,
            IAsyncSubscriber<MyMessage> asyncKeylessS,

            IRequestHandler<Ping, Pong> pingponghandler,
            //PingHandler pingpingHandler,
            IRequestAllHandler<Ping, Pong> pingallhandler,

            IPublisher<int> intP,
            ISubscriber<int> intS,
            IServiceScopeFactory scopeF,
            MessagePipeDiagnosticsInfo diagnosticsInfo,

            IServiceProvider provider


            )
        {
            this.provider = provider;
            this.scopeF = scopeF;
            this.publisher = publisher;
            this.subscriber = subscriber;
            this.keylessP = keyless1;
            this.keylessS = keyless2;
            this.asyncKeylessP = asyncKeylessP;
            this.asyncKeylessS = asyncKeylessS;
            this.pingponghandler = pingponghandler;
            //this.pingpingHandler = pingpingHandler;
            this.pingallhandler = pingallhandler;
            this.intPublisher = intP;
            this.intSubscriber = intS;
            this.diagnosticsInfo = diagnosticsInfo;

            var r1 = provider.GetRequiredService<IRequestHandler<string, int>>();
            r1.Invoke("foo");
        }

        [Command("keyed")]
        public void Keyed()
        {
            this.subscriber.Subscribe("foo", x =>
            {
                Console.WriteLine("A:" + x.MyProperty);
            });

            this.subscriber.Subscribe("foo", new MyFirst());

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


            keylessS.AsObservable();


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
            var pong = pingponghandler.Invoke(new Ping());
            Console.WriteLine("pong");
        }

        [Command("pingmany")]
        public void PingMany()
        {
            Console.WriteLine("ping");
            var pong = pingallhandler.InvokeAll(new Ping());
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
            var d = DisposableBag.CreateBuilder();
            this.keylessS.Subscribe(x =>
            {
                Console.WriteLine("FilteredA:" + x.MyProperty);
            }, x => x.MyProperty == "foo" || x.MyProperty == "hoge")
                .AddTo(d);


            this.keylessS.Subscribe(x =>
            {
                Console.WriteLine("FilteredB:" + x.MyProperty);
            }, x => x.MyProperty == "foo" || x.MyProperty == "hage").AddTo(d);

            this.keylessP.Publish(new MyMessage { MyProperty = "nano" });
            this.keylessP.Publish(new MyMessage { MyProperty = "foo" });
            this.keylessP.Publish(new MyMessage { MyProperty = "hage" });
            this.keylessP.Publish(new MyMessage { MyProperty = "hoge" });

            this.intSubscriber.Subscribe(x => Console.WriteLine(x), x => x < 10).AddTo(d);
            this.intPublisher.Publish(999);
            this.intPublisher.Publish(5);

            d.Build().Dispose();
            d.Clear();
            Console.WriteLine("----");

            intSubscriber.Subscribe(x =>
            {
                Console.WriteLine("int one:" + x);
            }, new ChangedValueFilter<int>());

            intPublisher.Publish(100);
            intPublisher.Publish(200);
            intPublisher.Publish(200);
            intPublisher.Publish(299);


        }

        [Command("checkscope")]
        public void CheckScope()
        {

            var scope = scopeF.CreateScope();

            var scope2 = scopeF.CreateScope();

            var p = scope.ServiceProvider.GetRequiredService<IPublisher<long>>();
            var s = scope.ServiceProvider.GetRequiredService<ISubscriber<long>>();

            var p2 = scope2.ServiceProvider.GetRequiredService<IPublisher<long>>();
            var s2 = scope2.ServiceProvider.GetRequiredService<ISubscriber<long>>();

            var d = s.Subscribe(x => Console.WriteLine("foo:" + x));
            var d2 = s2.Subscribe(x => Console.WriteLine("bar:" + x));



            p.Publish(100);
            p.Publish(200);
            p.Publish(300);
            p2.Publish(999);


            scope.Dispose();

            p.Publish(129);
            s.Subscribe(_ => Console.WriteLine("s2???"));

            p2.Publish(1999);
        }

        [Command("moremore")]
        public void CheckMoreAndMore()
        {
            var req = provider.GetRequiredService<IRequestHandler<int, int>>();
            var all = provider.GetRequiredService<IRequestAllHandler<int, int>>();


            req.Invoke(100);


            intPublisher.Publish(10);
        }
    }



    public class PingHandler : IRequestHandler<Ping, Pong>
    {
        public Pong Invoke(Ping request)
        {
            Console.WriteLine("1 ping");
            return new Pong();
        }
    }

    public class PingHandler2 : IRequestHandler<Ping, Pong>
    {
        public Pong Invoke(Ping request)
        {
            Console.WriteLine("2 ping");
            return new Pong();
        }
    }


    public class MyClass
    {

    }


    public class MyFilter<T> : MessageHandlerFilter<T>
    {
        public override void Handle(T message, Action<T> next)
        {
            Console.WriteLine("before:" + Order);
            next(message);
            Console.WriteLine("after:" + Order);
        }
    }

    [MessageHandlerFilter(typeof(MyFilter<MyMessage>), Order = 30)]
    [MessageHandlerFilter(typeof(MyFilter<MyMessage>), Order = -99)]
    [MessageHandlerFilter(typeof(MyFilter<MyMessage>), Order = 1000)]
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



    public class FirstHandler : IRequestHandler<int, int>
    {
        public int Invoke(int request)
        {
            Console.WriteLine("Called First!");
            return request;
        }
    }

    public class SecondHandler : IRequestHandler<int, int>
    {
        public int Invoke(int request)
        {
            Console.WriteLine("Called Second!");
            return request;
        }
    }

    public class ThirdHandler : IRequestHandler<int, int>
    {
        public int Invoke(int request)
        {
            Console.WriteLine("Called Third!");
            return request;
        }
    }



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
                Console.WriteLine(diagnosticsInfo.SubscribeCount);
                await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
            }
        }

        public void Dispose()
        {
            cts.Cancel();
        }
    }


    [MessageHandlerFilter(typeof(ChangedValueFilter<>))]
    public class WriteLineHandler<T> : IMessageHandler<T>
    {
        public void Handle(T message) => Console.WriteLine(message);
    }


    public class DelayRequestFilter : AsyncRequestHandlerFilter<int, int>
    {
        public override async ValueTask<int> InvokeAsync(int request, CancellationToken cancellationToken, Func<int, CancellationToken, ValueTask<int>> next)
        {
            await Task.Delay(TimeSpan.FromSeconds(request));
            var response = await next(request, cancellationToken);
            return response;
        }
    }

    public class Command1
    {
    }

    public class Response1
    {
    }

    public class Command2
    {
    }

    public class Response2
    {
    }


    public class MultiHandler :
        IAsyncRequestHandler<Command1, Response1>,
        IAsyncRequestHandler<Command2, Response2>
    {
        public ValueTask<Response1> InvokeAsync(Command1 request, CancellationToken cancellationToken = default)
        {
            return default;
        }

        public ValueTask<Response2> InvokeAsync(Command2 request, CancellationToken cancellationToken = default)
        {
            return default;
        }
    }


}

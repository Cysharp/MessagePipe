#pragma warning disable CS1998
#pragma warning disable MPA001 // for check MessagePipe.Analyzer, remove this line.

using System;
using System.Linq;
using System.Reactive.Linq;

namespace MessagePipe.AnalyzerTestApp
{

    class Program
    {
        static void Main()
        {
            Observable.Range(1, 10).Subscribe(_ => Console.WriteLine("OK Rx"));
        }

        static IDisposable Ret(ISubscriber<int> subscriber)
        {
            return subscriber.Subscribe(_ => Console.WriteLine("OK RETURN"));
        }

        static IDisposable Ret(ISubscriber<int, int> subscriber)
        {
            return subscriber.Subscribe(0, _ => Console.WriteLine("OK RETURN"));
        }

        static IDisposable Ret(IAsyncSubscriber<int> subscriber)
        {
            return subscriber.Subscribe(async (_, _) => Console.WriteLine("OK RETURN"));
        }

        static IDisposable Ret(IAsyncSubscriber<int, int> subscriber)
        {
            return subscriber.Subscribe(0, async (_, _) => Console.WriteLine("OK RETURN"));
        }

        static IDisposable Ret(IBufferedSubscriber<int> subscriber)
        {
            return subscriber.Subscribe(_ => Console.WriteLine("OK RETURN"));
        }

        class MyClass
        {
            public MyClass(IDisposable d)
            {

            }
        }

        static void Subscriber(ISubscriber<int> subscriber)
        {
            subscriber.Subscribe(_ => Console.WriteLine("NG NOT HANDLED"));

            var d = subscriber.Subscribe(_ => Console.WriteLine("OK ASSIGN"));

            IDisposable d1;
            d1 = subscriber.Subscribe(_ => Console.WriteLine("OK ASSIGN2"));

            _ = subscriber.Subscribe(_ => Console.WriteLine("OK ASSIGN3"));

            DisposableBag.Create(subscriber.Subscribe(_ => Console.WriteLine("OK METHOD ARG1")));

            var bag = DisposableBag.CreateBuilder();
            subscriber.Subscribe(_ => Console.WriteLine("OK METHOD CHAIN")).AddTo(bag);

            Enumerable.Range(1, 10).Select(_ => subscriber.Subscribe(_ => Console.WriteLine("OK IN LAMBDA")));

            new MyClass(subscriber.Subscribe(_ => Console.WriteLine("OK CTOR")));

            using (subscriber.Subscribe(_ => Console.WriteLine("OK USING")))
            {
            }

            using (subscriber.Subscribe(_ => Console.WriteLine("OK USING2")))
            {
            }

            using var u = subscriber.Subscribe(_ => Console.WriteLine("OK USING 3"));
        }

        static void Subscriber2(ISubscriber<string, int> subscriber)
        {
            subscriber.Subscribe("a", _ => Console.WriteLine("NG NOT HANDLED"));

            var d = subscriber.Subscribe("a", _ => Console.WriteLine("OK ASSIGN"));

            IDisposable d1;
            d1 = subscriber.Subscribe("a", _ => Console.WriteLine("OK ASSIGN2"));

            _ = subscriber.Subscribe("a", _ => Console.WriteLine("OK ASSIGN3"));

            DisposableBag.Create(subscriber.Subscribe("a", _ => Console.WriteLine("OK METHOD ARG1")));

            var bag = DisposableBag.CreateBuilder();
            subscriber.Subscribe("a", _ => Console.WriteLine("OK METHOD CHAIN")).AddTo(bag);

            Enumerable.Range(1, 10).Select(_ => subscriber.Subscribe("a", _ => Console.WriteLine("OK IN LAMBDA")));

            new MyClass(subscriber.Subscribe("a", _ => Console.WriteLine("OK CTOR")));

            using (subscriber.Subscribe("a", _ => Console.WriteLine("OK USING")))
            {
            }

            using (subscriber.Subscribe("a", _ => Console.WriteLine("OK USING2")))
            {
            }

            using var u = subscriber.Subscribe("a", _ => Console.WriteLine("OK USING 3"));
        }

        static void AsyncSubscriber(IAsyncSubscriber<int> subscriber)
        {
            subscriber.Subscribe(async (_, _) => Console.WriteLine("NG NOT HANDLED"));

            var d = subscriber.Subscribe(async (_, _) => Console.WriteLine("OK ASSIGN"));

            IDisposable d1;
            d1 = subscriber.Subscribe(async (_, _) => Console.WriteLine("OK ASSIGN2"));

            _ = subscriber.Subscribe(async (_, _) => Console.WriteLine("OK ASSIGN3"));

            DisposableBag.Create(subscriber.Subscribe(async (_, _) => Console.WriteLine("OK METHOD ARG1")));

            var bag = DisposableBag.CreateBuilder();
            subscriber.Subscribe(async (_, _) => Console.WriteLine("OK METHOD CHAIN")).AddTo(bag);

            Enumerable.Range(1, 10).Select(_ => subscriber.Subscribe(async (_, _) => Console.WriteLine("OK IN LAMBDA")));

            new MyClass(subscriber.Subscribe(async (_, _) => Console.WriteLine("OK CTOR")));

            using (subscriber.Subscribe(async (_, _) => Console.WriteLine("OK USING")))
            {
            }

            using (subscriber.Subscribe(async (_, _) => Console.WriteLine("OK USING2")))
            {
            }

            using var u = subscriber.Subscribe(async (_, _) => Console.WriteLine("OK USING 3"));
        }

        static void AsyncSubscriber2(IAsyncSubscriber<string, int> subscriber)
        {
            subscriber.Subscribe("a", async (_, _) => Console.WriteLine("NG NOT HANDLED"));

            var d = subscriber.Subscribe("a", async (_, _) => Console.WriteLine("OK ASSIGN"));

            IDisposable d1;
            d1 = subscriber.Subscribe("a", async (_, _) => Console.WriteLine("OK ASSIGN2"));

            _ = subscriber.Subscribe("a", async (_, _) => Console.WriteLine("OK ASSIGN3"));

            DisposableBag.Create(subscriber.Subscribe("a", async (_, _) => Console.WriteLine("OK METHOD ARG1")));

            var bag = DisposableBag.CreateBuilder();
            subscriber.Subscribe("a", async (_, _) => Console.WriteLine("OK METHOD CHAIN")).AddTo(bag);

            Enumerable.Range(1, 10).Select(async (_, _) => subscriber.Subscribe("a", async (_, _) => Console.WriteLine("OK IN LAMBDA")));

            new MyClass(subscriber.Subscribe("a", async (_, _) => Console.WriteLine("OK CTOR")));

            using (subscriber.Subscribe("a", async (_, _) => Console.WriteLine("OK USING")))
            {
            }

            using (subscriber.Subscribe("a", async (_, _) => Console.WriteLine("OK USING2")))
            {
            }

            using var u = subscriber.Subscribe("a", async (_, _) => Console.WriteLine("OK USING 3"));
        }

        static void BufferedSubscriber(IBufferedSubscriber<int> subscriber)
        {
            subscriber.Subscribe(_ => Console.WriteLine("NG NOT HANDLED"));

            var d = subscriber.Subscribe(_ => Console.WriteLine("OK ASSIGN"));

            IDisposable d1;
            d1 = subscriber.Subscribe(_ => Console.WriteLine("OK ASSIGN2"));

            _ = subscriber.Subscribe(_ => Console.WriteLine("OK ASSIGN3"));

            DisposableBag.Create(subscriber.Subscribe(_ => Console.WriteLine("OK METHOD ARG1")));

            var bag = DisposableBag.CreateBuilder();
            subscriber.Subscribe(_ => Console.WriteLine("OK METHOD CHAIN")).AddTo(bag);

            Enumerable.Range(1, 10).Select(_ => subscriber.Subscribe(_ => Console.WriteLine("OK IN LAMBDA")));

            new MyClass(subscriber.Subscribe(_ => Console.WriteLine("OK CTOR")));

            using (subscriber.Subscribe(_ => Console.WriteLine("OK USING")))
            {
            }

            using (subscriber.Subscribe(_ => Console.WriteLine("OK USING2")))
            {
            }

            using var u = subscriber.Subscribe(_ => Console.WriteLine("OK USING 3"));
        }
    }
}

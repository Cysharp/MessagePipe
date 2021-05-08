#pragma warning disable CS1998
#pragma warning disable MPA001 // for check MessagePipe.Analyzer, remove this line.

using System;
using System.Linq;
using System.Reactive.Linq;

namespace MessagePipe.AnalyzerTestApp
{

    class Program
    {
        static void Main(string[] args)
        {
            Observable.Range(1, 10).Subscribe(x => Console.WriteLine("OK Rx"));
        }

        static IDisposable Ret(ISubscriber<int> subscriber)
        {
            return subscriber.Subscribe(x => Console.WriteLine("OK RETURN"));
        }

        static IDisposable Ret(ISubscriber<int, int> subscriber)
        {
            return subscriber.Subscribe(0, x => Console.WriteLine("OK RETURN"));
        }

        static IDisposable Ret(IAsyncSubscriber<int> subscriber)
        {
            return subscriber.Subscribe(async (x, ct) => Console.WriteLine("OK RETURN"));
        }

        static IDisposable Ret(IAsyncSubscriber<int, int> subscriber)
        {
            return subscriber.Subscribe(0, async (x, ct) => Console.WriteLine("OK RETURN"));
        }

        static IDisposable Ret(IBufferedSubscriber<int> subscriber)
        {
            return subscriber.Subscribe(x => Console.WriteLine("OK RETURN"));
        }

        class MyClass
        {
            public MyClass(IDisposable d)
            {

            }
        }

        static void Subscriber(ISubscriber<int> subscriber)
        {
            subscriber.Subscribe(x => Console.WriteLine("NG NOT HANDLED"));

            var d = subscriber.Subscribe(x => Console.WriteLine("OK ASSIGN"));

            IDisposable d1;
            d1 = subscriber.Subscribe(x => Console.WriteLine("OK ASSIGN2"));

            _ = subscriber.Subscribe(x => Console.WriteLine("OK ASSIGN3"));

            DisposableBag.Create(subscriber.Subscribe(x => Console.WriteLine("OK METHOD ARG1")));

            var bag = DisposableBag.CreateBuilder();
            subscriber.Subscribe(x => Console.WriteLine("OK METHOD CHAIN")).AddTo(bag);

            Enumerable.Range(1, 10).Select(x => subscriber.Subscribe(y => Console.WriteLine("OK IN LAMBDA")));

            new MyClass(subscriber.Subscribe(x => Console.WriteLine("OK CTOR")));

            using (subscriber.Subscribe(x => Console.WriteLine("OK USING")))
            {
            }

            using (var uu = subscriber.Subscribe(x => Console.WriteLine("OK USING2")))
            {
            }

            using var u = subscriber.Subscribe(x => Console.WriteLine("OK USING 3"));
        }

        static void Subscriber2(ISubscriber<string, int> subscriber)
        {
            subscriber.Subscribe("a", x => Console.WriteLine("NG NOT HANDLED"));

            var d = subscriber.Subscribe("a", x => Console.WriteLine("OK ASSIGN"));

            IDisposable d1;
            d1 = subscriber.Subscribe("a", x => Console.WriteLine("OK ASSIGN2"));

            _ = subscriber.Subscribe("a", x => Console.WriteLine("OK ASSIGN3"));

            DisposableBag.Create(subscriber.Subscribe("a", x => Console.WriteLine("OK METHOD ARG1")));

            var bag = DisposableBag.CreateBuilder();
            subscriber.Subscribe("a", x => Console.WriteLine("OK METHOD CHAIN")).AddTo(bag);

            Enumerable.Range(1, 10).Select(x => subscriber.Subscribe("a", y => Console.WriteLine("OK IN LAMBDA")));

            new MyClass(subscriber.Subscribe("a", x => Console.WriteLine("OK CTOR")));

            using (subscriber.Subscribe("a", x => Console.WriteLine("OK USING")))
            {
            }

            using (var uu = subscriber.Subscribe("a", x => Console.WriteLine("OK USING2")))
            {
            }

            using var u = subscriber.Subscribe("a", x => Console.WriteLine("OK USING 3"));
        }

        static void AsyncSubscriber(IAsyncSubscriber<int> subscriber)
        {
            subscriber.Subscribe(async (x, ct) => Console.WriteLine("NG NOT HANDLED"));

            var d = subscriber.Subscribe(async (x, ct) => Console.WriteLine("OK ASSIGN"));

            IDisposable d1;
            d1 = subscriber.Subscribe(async (x, ct) => Console.WriteLine("OK ASSIGN2"));

            _ = subscriber.Subscribe(async (x, ct) => Console.WriteLine("OK ASSIGN3"));

            DisposableBag.Create(subscriber.Subscribe(async (x, ct) => Console.WriteLine("OK METHOD ARG1")));

            var bag = DisposableBag.CreateBuilder();
            subscriber.Subscribe(async (x, ct) => Console.WriteLine("OK METHOD CHAIN")).AddTo(bag);

            Enumerable.Range(1, 10).Select(x => subscriber.Subscribe(async (y, ct) => Console.WriteLine("OK IN LAMBDA")));

            new MyClass(subscriber.Subscribe(async (x, ct) => Console.WriteLine("OK CTOR")));

            using (subscriber.Subscribe(async (x, ct) => Console.WriteLine("OK USING")))
            {
            }

            using (var uu = subscriber.Subscribe(async (x, ct) => Console.WriteLine("OK USING2")))
            {
            }

            using var u = subscriber.Subscribe(async (x, ct) => Console.WriteLine("OK USING 3"));
        }

        static void AsyncSubscriber2(IAsyncSubscriber<string, int> subscriber)
        {
            subscriber.Subscribe("a", async (x, ct) => Console.WriteLine("NG NOT HANDLED"));

            var d = subscriber.Subscribe("a", async (x, ct) => Console.WriteLine("OK ASSIGN"));

            IDisposable d1;
            d1 = subscriber.Subscribe("a", async (x, ct) => Console.WriteLine("OK ASSIGN2"));

            _ = subscriber.Subscribe("a", async (x, ct) => Console.WriteLine("OK ASSIGN3"));

            DisposableBag.Create(subscriber.Subscribe("a", async (x, ct) => Console.WriteLine("OK METHOD ARG1")));

            var bag = DisposableBag.CreateBuilder();
            subscriber.Subscribe("a", async (x, ct) => Console.WriteLine("OK METHOD CHAIN")).AddTo(bag);

            Enumerable.Range(1, 10).Select(async (x, ct) => subscriber.Subscribe("a", async (y, ct) => Console.WriteLine("OK IN LAMBDA")));

            new MyClass(subscriber.Subscribe("a", async (x, ct) => Console.WriteLine("OK CTOR")));

            using (subscriber.Subscribe("a", async (x, ct) => Console.WriteLine("OK USING")))
            {
            }

            using (var uu = subscriber.Subscribe("a", async (x, ct) => Console.WriteLine("OK USING2")))
            {
            }

            using var u = subscriber.Subscribe("a", async (x, ct) => Console.WriteLine("OK USING 3"));
        }

        static void BufferedSubscriber(IBufferedSubscriber<int> subscriber)
        {
            subscriber.Subscribe(x => Console.WriteLine("NG NOT HANDLED"));

            var d = subscriber.Subscribe(x => Console.WriteLine("OK ASSIGN"));

            IDisposable d1;
            d1 = subscriber.Subscribe(x => Console.WriteLine("OK ASSIGN2"));

            _ = subscriber.Subscribe(x => Console.WriteLine("OK ASSIGN3"));

            DisposableBag.Create(subscriber.Subscribe(x => Console.WriteLine("OK METHOD ARG1")));

            var bag = DisposableBag.CreateBuilder();
            subscriber.Subscribe(x => Console.WriteLine("OK METHOD CHAIN")).AddTo(bag);

            Enumerable.Range(1, 10).Select(x => subscriber.Subscribe(y => Console.WriteLine("OK IN LAMBDA")));

            new MyClass(subscriber.Subscribe(x => Console.WriteLine("OK CTOR")));

            using (subscriber.Subscribe(x => Console.WriteLine("OK USING")))
            {
            }

            using (var uu = subscriber.Subscribe(x => Console.WriteLine("OK USING2")))
            {
            }

            using var u = subscriber.Subscribe(x => Console.WriteLine("OK USING 3"));
        }
    }
}

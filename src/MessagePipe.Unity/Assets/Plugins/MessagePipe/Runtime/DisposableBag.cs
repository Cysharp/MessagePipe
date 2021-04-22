using System;
using System.Collections.Generic;

namespace MessagePipe
{
    public static partial class DisposableBag
    {
        public static IDisposable Create(params IDisposable[] disposables)
        {
            return new NthDisposable(disposables);
        }

        sealed class NthDisposable : IDisposable
        {
            bool disposed;
            readonly IDisposable[] disposables;

            public NthDisposable(IDisposable[] disposables)
            {
                this.disposables = disposables;
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                    foreach (var item in disposables)
                    {
                        item.Dispose();
                    }
                }
            }
        }

        public static DisposableBagBuilder CreateBuilder()
        {
            return new DisposableBagBuilder();
        }

        public static DisposableBagBuilder CreateBuilder(int initialCapacity)
        {
            return new DisposableBagBuilder(initialCapacity);
        }

        public static IDisposable Empty => EmptyDisposable.Instance;

        public static void AddTo(this IDisposable disposable, DisposableBagBuilder disposableBag)
        {
            disposableBag.Add(disposable);
        }
    }

    internal class EmptyDisposable : IDisposable
    {
        internal static readonly IDisposable Instance = new EmptyDisposable();

        EmptyDisposable()
        {
        }

        public void Dispose()
        {
        }
    }

    public partial class DisposableBagBuilder
    {
        readonly List<IDisposable> disposables;

        internal DisposableBagBuilder()
        {
            disposables = new List<IDisposable>();
        }

        internal DisposableBagBuilder(int initialCapacity)
        {
            disposables = new List<IDisposable>(initialCapacity);
        }

        public void Add(IDisposable disposable)
        {
            disposables.Add(disposable);
        }

        public void Clear()
        {
            disposables.Clear();
        }

        //public IDisposable Build() in Disposables.tt(Disposables.cs)
    }
}

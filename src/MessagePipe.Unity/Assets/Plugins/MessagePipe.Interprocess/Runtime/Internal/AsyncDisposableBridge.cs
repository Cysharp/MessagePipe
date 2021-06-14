using System;
using Cysharp.Threading.Tasks;

namespace MessagePipe.Interprocess.Internal
{
    internal sealed class AsyncDisposableBridge : IUniTaskAsyncDisposable
    {
        readonly IDisposable disposable;

        public AsyncDisposableBridge(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public UniTask DisposeAsync()
        {
            disposable.Dispose();
            return default;
        }
    }

    
}

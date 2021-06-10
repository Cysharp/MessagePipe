using System;
using System.Threading.Tasks;

namespace MessagePipe.InProcess.Internal
{
    internal sealed class AsyncDisposableBridge : IAsyncDisposable
    {
        readonly IDisposable disposable;

        public AsyncDisposableBridge(IDisposable disposable)
        {
            this.disposable = disposable;
        }

        public ValueTask DisposeAsync()
        {
            disposable.Dispose();
            return default;
        }
    }

    
}

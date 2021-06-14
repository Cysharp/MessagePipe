using MessagePipe.InProcess.Internal;
using MessagePipe.InProcess.Workers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe.InProcess
{
    [Preserve]
    public sealed class TcpRemoteRequestHandler<TRequest, TResponse> : IRemoteRequestHandler<TRequest, TResponse>
    {
        readonly TcpWorker worker;

        [Preserve]
        public TcpRemoteRequestHandler(TcpWorker worker)
        {
            this.worker = worker;
        }

        public async ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return await worker.RequestAsync<TRequest, TResponse>(request, cancellationToken);
        }
    }
}

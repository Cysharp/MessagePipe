using MessagePipe.Interprocess.Internal;
using MessagePipe.Interprocess.Workers;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe.Interprocess
{
    [Preserve]
    public sealed class NamedPipeRemoteRequestHandler<TRequest, TResponse> : IRemoteRequestHandler<TRequest, TResponse>
    {
        readonly NamedPipeWorker worker;

        [Preserve]
        public NamedPipeRemoteRequestHandler(NamedPipeWorker worker)
        {
            this.worker = worker;
        }

        public async UniTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return await worker.RequestAsync<TRequest, TResponse>(request, cancellationToken);
        }
    }
}

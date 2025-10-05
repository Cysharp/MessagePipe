using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    public sealed class StreamRequestHandler<TRequest, TResponse> : IStreamRequestHandler<TRequest, TResponse>
    {
        private readonly IStreamRequestHandler<TRequest, TResponse> handler;

        public StreamRequestHandler(IStreamRequestHandler<TRequest, TResponse> handler)
        {
            this.handler = handler;
        }

        public IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return handler.HandleAsync(request, cancellationToken);
        }
    }
}

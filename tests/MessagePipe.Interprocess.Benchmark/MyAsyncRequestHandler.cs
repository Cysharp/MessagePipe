using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MessagePipe.Interprocess.Benchmark
{
    public class MyAsyncMessageHandler : IAsyncMessageHandler<byte[]>
    {
        public ValueTask HandleAsync(byte[] message, CancellationToken cancellationToken)
        {
            return default(ValueTask);
        }
    }
    public class MyAsyncHandler : IAsyncRequestHandler<int, byte[]>
    {
        public ValueTask<byte[]> InvokeAsync(int request, CancellationToken cancellationToken = default)
        {
            if (request == -1)
            {
                throw new Exception("NO -1");
            }
            else
            {
                return new ValueTask<byte[]>(new byte[request]);
            }
        }
    }
}

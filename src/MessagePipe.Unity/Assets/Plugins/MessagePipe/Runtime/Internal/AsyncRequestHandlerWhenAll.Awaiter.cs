using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace MessagePipe.Internal
{
    internal partial class AsyncRequestHandlerWhenAll<TRequest, TResponse>
    {
        internal class AwaiterNode : IPoolStackNode<AwaiterNode>
        {
            AwaiterNode nextNode;
            public ref AwaiterNode NextNode => ref nextNode;

            AsyncRequestHandlerWhenAll<TRequest, TResponse> parent = default;
            Cysharp.Threading.Tasks.UniTask<TResponse>.Awaiter awaiter;
            int index = -1;

            readonly Action continuation;

            static PoolStack<AwaiterNode> pool;

            public AwaiterNode()
            {
                this.continuation = OnCompleted;
            }

            public static void RegisterUnsafeOnCompleted(AsyncRequestHandlerWhenAll<TRequest, TResponse> parent, Cysharp.Threading.Tasks.UniTask<TResponse>.Awaiter awaiter, int index)
            {
                if (!pool.TryPop(out var result))
                {
                    result = new AwaiterNode();
                }
                result.parent = parent;
                result.awaiter = awaiter;
                result.index = index;

                result.awaiter.UnsafeOnCompleted(result.continuation);
            }

            void OnCompleted()
            {
                var p = this.parent;
                var a = this.awaiter;
                var i = this.index;
                this.parent = null;
                this.awaiter = default;
                this.index = -1;

                pool.TryPush(this);

                try
                {
                    p.result[i] = a.GetResult();
                }
                catch (Exception ex)
                {
                    p.exception = ExceptionDispatchInfo.Capture(ex);
                    p.TryInvokeContinuation();
                    return;
                }

                p.IncrementSuccessfully();
            }
        }
    }
}

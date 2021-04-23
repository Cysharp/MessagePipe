using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace MessagePipe.Internal
{
    internal partial class AsyncHandlerWhenAll<T>
    {
        internal class AwaiterNode : IPoolStackNode<AwaiterNode>
        {
            AwaiterNode nextNode;
            public ref AwaiterNode NextNode => ref nextNode;

            AsyncHandlerWhenAll<T> parent = default;
            Cysharp.Threading.Tasks.UniTask.Awaiter awaiter;

            readonly Action continuation;

            static PoolStack<AwaiterNode> pool;

            public AwaiterNode()
            {
                this.continuation = OnCompleted;
            }

            public static void RegisterUnsafeOnCompleted(AsyncHandlerWhenAll<T> parent, Cysharp.Threading.Tasks.UniTask.Awaiter awaiter)
            {
                if (!pool.TryPop(out var result))
                {
                    result = new AwaiterNode();
                }
                result.parent = parent;
                result.awaiter = awaiter;

                result.awaiter.UnsafeOnCompleted(result.continuation);
            }

            void OnCompleted()
            {
                var p = this.parent;
                var a = this.awaiter;
                this.parent = null;
                this.awaiter = default;

                pool.TryPush(this);

                try
                {
                    a.GetResult();
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

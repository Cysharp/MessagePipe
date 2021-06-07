using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace MessagePipe.Internal
{
    internal partial class AsyncRequestHandlerWhenAll<TRequest, TResponse> : ICriticalNotifyCompletion
    {
        int completedCount;
        ExceptionDispatchInfo exception;
        Action continuation = ContinuationSentinel.AvailableContinuation;

        readonly TResponse[] result;

        public AsyncRequestHandlerWhenAll(IAsyncRequestHandlerCore<TRequest, TResponse>[] handlers, TRequest request, CancellationToken cancellationtoken)
        {
            result = new TResponse[handlers.Length];

            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    var awaiter = handlers[i].InvokeAsync(request, cancellationtoken).GetAwaiter();
                    if (awaiter.IsCompleted)
                    {
                        result[i] = awaiter.GetResult();
                    }
                    else
                    {
                        AwaiterNode.RegisterUnsafeOnCompleted(this, awaiter, i);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    exception = ExceptionDispatchInfo.Capture(ex);
                    TryInvokeContinuation();
                    return;
                }

                IncrementSuccessfully();
            }
        }

        void IncrementSuccessfully()
        {
            if (Interlocked.Increment(ref completedCount) == result.Length)
            {
                TryInvokeContinuation();
            }
        }

        void TryInvokeContinuation()
        {
            var c = Interlocked.Exchange(ref continuation, ContinuationSentinel.CompletedContinuation); // register completed.
            if (c != ContinuationSentinel.AvailableContinuation && c != ContinuationSentinel.CompletedContinuation)
            {
                c();
            }
        }

        // Awaiter

        public AsyncRequestHandlerWhenAll<TRequest, TResponse> GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted => exception != null || completedCount == result.Length;

        public TResponse[] GetResult()
        {
            if (exception != null)
            {
                exception.Throw();
            }
            // Complete, OK.
            return result;
        }

        public void OnCompleted(Action continuation)
        {
            UnsafeOnCompleted(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            var c = Interlocked.CompareExchange(ref this.continuation, continuation, ContinuationSentinel.AvailableContinuation);
            if (c == ContinuationSentinel.CompletedContinuation) // registered TryInvokeContinuation first.
            {
                continuation();
            }
        }
    }
}

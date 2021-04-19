using System;
#if !UNITY_2018_3_OR_NEWER
using System.Runtime.CompilerServices;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe
{
    internal sealed class PredicateFilter<T> : MessageHandlerFilter
    {
        readonly Func<T, bool> predicate;

        public PredicateFilter(Func<T, bool> predicate)
        {
            this.predicate = predicate;
            this.Order = int.MinValue; // filter first.
        }

        // T and T2 should be same.
        public override void Handle<T2>(T2 message, Action<T2> next)
        {
#if UNITY_2018_3_OR_NEWER
            if (predicate((T)(object)message))
#else
            if (predicate(Unsafe.As<T2, T>(ref message)))
#endif
            {
                next(message);
            }
        }
    }

    internal sealed class AsyncPredicateFilter<T> : AsyncMessageHandlerFilter
    {
        readonly Func<T, bool> predicate;

        public AsyncPredicateFilter(Func<T, bool> predicate)
        {
            this.predicate = predicate;
            this.Order = int.MinValue; // filter first.
        }

        public override ValueTask HandleAsync<T2>(T2 message, CancellationToken cancellationToken, Func<T2, CancellationToken, ValueTask> next)
        {
#if UNITY_2018_3_OR_NEWER
            if (predicate((T)(object)message))
#else
            if (predicate(Unsafe.As<T2, T>(ref message)))
#endif
            {
                return next(message, cancellationToken);
            }
            return default(ValueTask);
        }
    }
}
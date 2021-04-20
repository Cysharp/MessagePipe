using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    internal sealed class PredicateFilter<T> : MessageHandlerFilter<T>
    {
        readonly Func<T, bool> predicate;

        public PredicateFilter(Func<T, bool> predicate)
        {
            this.predicate = predicate;
            this.Order = int.MinValue; // predicate filter first.
        }

        public override void Handle(T message, Action<T> next)
        {
            if (predicate(message))
            {
                next(message);
            }
        }
    }

    internal sealed class AsyncPredicateFilter<T> : AsyncMessageHandlerFilter<T>
    {
        readonly Func<T, bool> predicate;

        public AsyncPredicateFilter(Func<T, bool> predicate)
        {
            this.predicate = predicate;
            this.Order = int.MinValue; // predicate filter first.
        }

        public override UniTask HandleAsync(T message, CancellationToken cancellationToken, Func<T, CancellationToken, UniTask> next)
        {
            if (predicate(message))
            {
                return next(message, cancellationToken);
            }
            return default(UniTask);
        }
    }
}
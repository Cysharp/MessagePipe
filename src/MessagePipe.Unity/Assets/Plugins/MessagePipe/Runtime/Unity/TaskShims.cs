using Cysharp.Threading.Tasks;

namespace MessagePipe
{
    internal static class TaskShims
    {
        internal static UniTask ConfigureAwait(this UniTask task, bool _)
        {
            return task;
        }

        internal static UniTask<T> ConfigureAwait<T>(this UniTask<T> task, bool _)
        {
            return task;
        }
    }
}

using System.Threading.Tasks;

namespace MessagePipe.Internal
{
    internal static class TaskExtensions
    {
        internal static async void Forget(this Task task)
        {
            await task;
        }
    }
}
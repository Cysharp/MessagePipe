﻿using System.Threading.Tasks;

namespace MessagePipe.Internal
{
    internal static class TaskExtensions
    {
#if !UNITY_2018_3_OR_NEWER

        internal static async void Forget(this ValueTask task)
        {
            await task;
        }

#endif
    }
}
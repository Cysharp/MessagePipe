using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MessagePipe
{
    public class MessagePipeDiagnosticsInfo
    {
        int subscribeCount;
        bool enableCaptureStackTrace;
        ConcurrentDictionary<IDisposable, string> capturedStackTraces = new ConcurrentDictionary<IDisposable, string>();

        public int SubscribeCount => subscribeCount;

        /// <summary>
        /// When MessagePipeOptions.EnableCaptureStackTrace is enabled, list all stacktrace on subscribe.
        /// </summary>
        public IEnumerable<string> CapturedStackTraces => capturedStackTraces.Select(x => x.Value);

        /// <summary>
        /// When MessagePipeOptions.EnableCaptureStackTrace is enabled, groped by caller of subscribe.
        /// </summary>
        public ILookup<string, string> GroupedByCaller
        {
            get
            {
                return CapturedStackTraces
                    .ToLookup(x =>
                    {
                        var split = x.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        var skips = split.SkipWhile(x => x.TrimStart().StartsWith("at MessagePipe"));
                        return skips.First().TrimStart().Substring(3); // remove "at ".
                    });
            }
        }

        public MessagePipeDiagnosticsInfo(MessagePipeOptions options)
        {
            if (options.EnableCaptureStackTrace)
            {
                enableCaptureStackTrace = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementSubscribe(IDisposable subscription)
        {
            Interlocked.Increment(ref subscribeCount);
            if (enableCaptureStackTrace)
            {
                capturedStackTraces.TryAdd(subscription, new StackTrace().ToString());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DecrementSubscribe(IDisposable subscription)
        {
            Interlocked.Decrement(ref subscribeCount);
            if (enableCaptureStackTrace)
            {
                capturedStackTraces.TryRemove(subscription, out _);
            }
        }
    }
}
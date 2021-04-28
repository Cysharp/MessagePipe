using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MessagePipe
{
    internal interface IHandlerHolderMarker
    {
    }

    /// <summary>
    /// Diagnostics info of in-memory(ISubscriber/IAsyncSubscriber) subscriptions.
    /// </summary>
    public sealed class MessagePipeDiagnosticsInfo
    {
        int subscribeCount;
        bool dirty;
        MessagePipeOptions options;

        object gate = new object();
        Dictionary<IHandlerHolderMarker, Dictionary<IDisposable, string>> capturedStackTraces = new Dictionary<IHandlerHolderMarker, Dictionary<IDisposable, string>>();

        /// <summary>Get current subscribed count.</summary>
        public int SubscribeCount => subscribeCount;

        internal bool CheckAndResetDirty()
        {
            var d = dirty;
            dirty = false;
            return d;
        }

        internal MessagePipeOptions MessagePipeOptions => options;

        /// <summary>
        /// When MessagePipeOptions.EnableCaptureStackTrace is enabled, list all stacktrace on subscribe.
        /// </summary>
        public string[] GetCapturedStackTraces()
        {
            if (!options.EnableCaptureStackTrace) return Array.Empty<string>();
            lock (gate)
            {
                return capturedStackTraces.SelectMany(x => x.Value.Values).ToArray();
            }
        }

        /// <summary>
        /// When MessagePipeOptions.EnableCaptureStackTrace is enabled, groped by caller of subscribe.
        /// </summary>
        public ILookup<string, string> GroupedByCaller
        {
            get
            {
                if (!options.EnableCaptureStackTrace) return Array.Empty<string>().ToLookup(x => x);
                lock (gate)
                {
                    return capturedStackTraces
                        .SelectMany(x => x.Value.Values)
                        .ToLookup(x =>
                        {
                            var split = x.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                            var skips = split.SkipWhile(y => y.TrimStart().Contains(" MessagePipe."));
                            return skips.First().TrimStart().Substring(3); // remove "at ".
                        });
                }
            }
        }

        public MessagePipeDiagnosticsInfo(MessagePipeOptions options)
        {
            this.options = options;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementSubscribe(IHandlerHolderMarker handlerHolder, IDisposable subscription)
        {
            Interlocked.Increment(ref subscribeCount);
            if (options.EnableCaptureStackTrace)
            {
                AddStackTrace(handlerHolder, subscription);
            }
            dirty = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void AddStackTrace(IHandlerHolderMarker handlerHolder, IDisposable subscription)
        {
            lock (gate)
            {
                if (!capturedStackTraces.TryGetValue(handlerHolder, out var dict))
                {
                    dict = new Dictionary<IDisposable, string>();
                    capturedStackTraces[handlerHolder] = dict;
                }

                dict.Add(subscription, new StackTrace().ToString());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DecrementSubscribe(IHandlerHolderMarker handlerHolder, IDisposable subscription)
        {
            Interlocked.Decrement(ref subscribeCount);
            if (options.EnableCaptureStackTrace)
            {
                RemoveStackTrace(handlerHolder, subscription);
            }
            dirty = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void RemoveStackTrace(IHandlerHolderMarker handlerHolder, IDisposable subscription)
        {
            lock (gate)
            {
                if (!capturedStackTraces.TryGetValue(handlerHolder, out var dict))
                {
                    return;
                }

                dict.Remove(subscription);
            }
        }

        internal void RemoveTargetDiagnostics(IHandlerHolderMarker targetHolder, int removeCount)
        {
            Interlocked.Add(ref subscribeCount, -removeCount);
            if (options.EnableCaptureStackTrace)
            {
                lock (gate)
                {
                    capturedStackTraces.Remove(targetHolder);
                }
            }
            dirty = true;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
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
        static bool displayFileNames = true;

        int subscribeCount;
        bool dirty;
        MessagePipeOptions options;

        object gate = new object();
        Dictionary<IHandlerHolderMarker, Dictionary<IDisposable, StackTrace>> capturedStackTraces = new Dictionary<IHandlerHolderMarker, Dictionary<IDisposable, StackTrace>>();

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
        public StackTrace[] GetCapturedStackTraces()
        {
            if (!options.EnableCaptureStackTrace) return Array.Empty<StackTrace>();
            lock (gate)
            {
                return capturedStackTraces.SelectMany(x => x.Value.Values).ToArray();
            }
        }

        /// <summary>
        /// When MessagePipeOptions.EnableCaptureStackTrace is enabled, groped by caller of subscribe.
        /// </summary>
        public ILookup<string, StackTrace> GroupedByCaller
        {
            get
            {
                if (!options.EnableCaptureStackTrace) return Array.Empty<StackTrace>().ToLookup(x => "", x => x);
                lock (gate)
                {
                    return capturedStackTraces
                        .SelectMany(x => x.Value.Values)
                        .ToLookup(x =>
                        {
                            return GetGroupKey(x);
                        });
                }
            }
        }

        internal static string GetGroupKey(StackTrace stackTrace)
        {
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var sf = stackTrace.GetFrame(i);
                if (sf == null) continue;
                var m = sf.GetMethod();
                if (m == null) continue;
                if (m.DeclaringType == null) continue;
                if (m.DeclaringType.Namespace == null || !m.DeclaringType.Namespace.StartsWith("MessagePipe"))
                {
                    if (displayFileNames && sf.GetILOffset() != -1)
                    {
                        string? fileName = null;
                        try
                        {
                            fileName = sf.GetFileName();
                        }
                        catch (NotSupportedException)
                        {
                            displayFileNames = false;
                        }
                        catch (SecurityException)
                        {
                            displayFileNames = false;
                        }

                        if (fileName != null)
                        {
                            return m.DeclaringType.FullName + "." + m.Name + " (at " + Path.GetFileName(fileName) + ":" + sf.GetFileLineNumber() + ")";
                        }
                        else
                        {
                            return m.DeclaringType.FullName + "." + m.Name + " (offset: " + sf.GetILOffset() + ")";
                        }
                    }

                    return m.DeclaringType.FullName + "." + m.Name;
                }
            }

            return "";
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
                    dict = new Dictionary<IDisposable, StackTrace>();
                    capturedStackTraces[handlerHolder] = dict;
                }

                dict.Add(subscription, new StackTrace(true));
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
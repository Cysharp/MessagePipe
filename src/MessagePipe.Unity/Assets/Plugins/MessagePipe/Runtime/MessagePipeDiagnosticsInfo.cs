using MessagePipe.Internal;
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

    public class StackTraceInfo
    {
        static bool displayFileNames = true;
        static int idSeed = 0;

        public int Id { get; }
        public DateTimeOffset Timestamp { get; }
        public StackTrace StackTrace { get; }
        public string Head { get; }

        internal string formattedStackTrace = default; // cache field for internal use(Unity Editor, etc...)

        public StackTraceInfo(StackTrace stackTrace)
        {
            Id = Interlocked.Increment(ref idSeed);
            Timestamp = DateTimeOffset.UtcNow;
            StackTrace = stackTrace;
            Head = GetGroupKey(stackTrace);
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
                        string fileName = null;
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
    }

    /// <summary>
    /// Diagnostics info of in-memory(ISubscriber/IAsyncSubscriber) subscriptions.
    /// </summary>
    [Preserve]
    public sealed class MessagePipeDiagnosticsInfo
    {
        static readonly ILookup<string, StackTraceInfo> EmptyLookup = Array.Empty<StackTraceInfo>().ToLookup(_ => "", x => x);

        int subscribeCount;
        bool dirty;
        MessagePipeOptions options;

        object gate = new object();
        Dictionary<IHandlerHolderMarker, Dictionary<IDisposable, StackTraceInfo>> capturedStackTraces = new Dictionary<IHandlerHolderMarker, Dictionary<IDisposable, StackTraceInfo>>();

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
        public StackTraceInfo[] GetCapturedStackTraces(bool ascending = true)
        {
            if (!options.EnableCaptureStackTrace) return Array.Empty<StackTraceInfo>();
            lock (gate)
            {
                var iter = capturedStackTraces.SelectMany(x => x.Value.Values);
                iter = (ascending) ? iter.OrderBy(x => x.Id) : iter.OrderByDescending(x => x.Id);
                return iter.ToArray();
            }
        }

        /// <summary>
        /// When MessagePipeOptions.EnableCaptureStackTrace is enabled, groped by caller of subscribe.
        /// </summary>
        public ILookup<string, StackTraceInfo> GetGroupedByCaller(bool ascending = true)
        {
            if (!options.EnableCaptureStackTrace) return EmptyLookup;
            lock (gate)
            {
                var iter = capturedStackTraces.SelectMany(x => x.Value.Values);
                iter = (ascending) ? iter.OrderBy(x => x.Id) : iter.OrderByDescending(x => x.Id);
                return iter.ToLookup(x => x.Head);
            }
        }

        [Preserve]
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
                    dict = new Dictionary<IDisposable, StackTraceInfo>();
                    capturedStackTraces[handlerHolder] = dict;
                }

                dict.Add(subscription, new StackTraceInfo(new StackTrace(true)));
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
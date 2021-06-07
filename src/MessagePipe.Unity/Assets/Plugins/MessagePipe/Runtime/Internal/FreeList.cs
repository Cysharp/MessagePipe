#pragma warning disable CS8618

using System;
using System.Collections.Generic;
using System.Threading;

namespace MessagePipe.Internal
{
    internal sealed class FreeList<T> : IDisposable
        where T : class
    {
        const int InitialCapacity = 4;
        const int MinShrinkStart = 8;

        T[] values;
        int count;
        FastQueue<int> freeIndex;
        bool isDisposed;
        readonly object gate = new object();

        public FreeList()
        {
            Initialize();
        }

        public T[] GetValues() => values; // no lock, safe for iterate

        public int GetCount()
        {
            lock (gate)
            {
                return count;
            }
        }

        public int Add(T value)
        {
            lock (gate)
            {
                if (isDisposed) throw new ObjectDisposedException(nameof(FreeList<T>));

                if (freeIndex.Count != 0)
                {
                    var index = freeIndex.Dequeue();
                    values[index] = value;
                    count++;
                    return index;
                }
                else
                {
                    // resize
                    var newValues = new T[values.Length * 2];
                    Array.Copy(values, 0, newValues, 0, values.Length);
                    freeIndex.EnsureNewCapacity(newValues.Length);
                    for (int i = values.Length; i < newValues.Length; i++)
                    {
                        freeIndex.Enqueue(i);
                    }

                    var index = freeIndex.Dequeue();
                    newValues[values.Length] = value;
                    count++;
                    Volatile.Write(ref values, newValues);
                    return index;
                }
            }
        }

        public void Remove(int index, bool shrinkWhenEmpty)
        {
            lock (gate)
            {
                if (isDisposed) return; // do nothing

                ref var v = ref values[index];
                if (v == null) throw new KeyNotFoundException($"key index {index} is not found.");

                v = null;
                freeIndex.Enqueue(index);
                count--;

                if (shrinkWhenEmpty && count == 0 && values.Length > MinShrinkStart)
                {
                    Initialize(); // re-init.
                }
            }
        }

        /// <summary>
        /// Dispose and get cleared count.
        /// </summary>
        public bool TryDispose(out int clearedCount)
        {
            lock (gate)
            {
                if (isDisposed)
                {
                    clearedCount = 0;
                    return false;
                }

                clearedCount = count;
                Dispose();
                return true;
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (isDisposed) return;
                isDisposed = true;

                freeIndex = null;
                values = Array.Empty<T>();
                count = 0;
            }
        }

        // [MemberNotNull(nameof(freeIndex), nameof(values))]
        void Initialize()
        {
            freeIndex = new FastQueue<int>(InitialCapacity);
            for (int i = 0; i < InitialCapacity; i++)
            {
                freeIndex.Enqueue(i);
            }
            count = 0;

            var v = new T[InitialCapacity];
            Volatile.Write(ref values, v);
        }
    }
}
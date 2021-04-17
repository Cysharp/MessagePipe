using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MessagePipe.Internal
{
    internal sealed class FreeList<TKey, TValue>
        where TKey : notnull
        where TValue : class
    {
        const int InitialCapacity = 4;
        const int LowerTrimExcess = 32;

        Dictionary<TKey, int> valueIndex;
        FixedSizeIntQueue freeIndex;
        TValue?[] values;
        int count;

        public TValue?[] GetUnsafeRawItems() => values;

        public int Count => count;

        public FreeList()
        {
            valueIndex = new Dictionary<TKey, int>(InitialCapacity);
            freeIndex = new FixedSizeIntQueue(InitialCapacity, 0, InitialCapacity);
            values = new TValue[InitialCapacity];
            count = 0;
        }

        public void Add(TKey key, TValue value)
        {
            if (freeIndex.Count != 0)
            {
                AddCore(freeIndex.Dequeue(), key, value);
            }
            else
            {
                var nextIndex = values.Length;
                Array.Resize(ref values, (int)(values.Length * 2));
                AddCore(nextIndex, key, value);

                freeIndex.EnsureNewCapacity(values.Length);
                for (int i = (nextIndex + 1); i < values.Length; i++)
                {
                    freeIndex.Enqueue(i);
                }
            }
        }

        void AddCore(int index, TKey key, TValue value)
        {
            valueIndex[key] = index;
            values[index] = value;
            count++;
        }

        public void Remove(TKey key)
        {
            // .NET 5 supports Remove(key, out var) but netstandard2.0 not.
            if (!valueIndex.TryGetValue(key, out var index)) return;

            values[index] = null;
            freeIndex.Enqueue(index);
            valueIndex.Remove(key);
            count--;

            if (values.Length >= LowerTrimExcess && count <= values.Length / 4)
            {
                TrimExcess();
            }
        }

        void TrimExcess()
        {
            var newValue = new TValue?[(int)(count * 2)];
            var newValueIndexes = new Dictionary<TKey, int>(newValue.Length);

            var i = 0;
            foreach (var item in valueIndex)
            {
                newValue[i] = values[item.Value];
                newValueIndexes.Add(item.Key, i);
                i++;
            }

            // set news.
            count = i;
            freeIndex = new FixedSizeIntQueue(newValue.Length, i, newValue.Length - i);
            values = newValue;
            valueIndex = newValueIndexes;
        }
    }

    internal sealed class FixedSizeIntQueue
    {
        int[] array;
        int head;
        int tail;
        int size;

        public FixedSizeIntQueue(int capacity, int start, int count)
        {
            array = new int[capacity];
            for (int tail = 0; tail < count; tail++)
            {
                array[tail] = start++;
            }

            if (tail == array.Length)
            {
                tail = 0;
            }
            size = count;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return size; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(int item)
        {
            array[tail] = item;
            MoveNext(ref tail);
            size++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Dequeue()
        {
            int head = this.head;
            int[] array = this.array;
            int removed = array[head];
            array[head] = default(int);
            MoveNext(ref this.head);
            size--;
            return removed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void MoveNext(ref int index)
        {
            int tmp = index + 1;
            if (tmp == array.Length)
            {
                tmp = 0;
            }
            index = tmp;
        }

        public void EnsureNewCapacity(int capacity)
        {
            var newarray = new int[capacity];
            if (size > 0)
            {
                if (head < tail)
                {
                    Array.Copy(array, head, newarray, 0, size);
                }
                else
                {
                    Array.Copy(array, head, newarray, 0, array.Length - head);
                    Array.Copy(array, 0, newarray, array.Length - head, tail);
                }
            }

            array = newarray;
            head = 0;
            tail = (size == capacity) ? 0 : size;
        }
    }
}
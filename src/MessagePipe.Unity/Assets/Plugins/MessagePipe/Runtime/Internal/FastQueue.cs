#pragma warning disable CS8618

using System;
using System.Runtime.CompilerServices;

namespace MessagePipe.Internal
{
    // fixed size queue.
    internal class FastQueue<T>
    {
        T[] array;
        int head;
        int tail;
        int size;

        public FastQueue(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException("capacity");
            array = new T[capacity];
            head = tail = size = 0;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return size; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            if (size == array.Length)
            {
                ThrowForFullQueue();
            }

            array[tail] = item;
            MoveNext(ref tail);
            size++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            if (size == 0) ThrowForEmptyQueue();

            int head = this.head;
            T[] array = this.array;
            T removed = array[head];
            array[head] = default;
            MoveNext(ref this.head);
            size--;
            return removed;
        }

        public void EnsureNewCapacity(int capacity)
        {
            T[] newarray = new T[capacity];
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

        void ThrowForEmptyQueue()
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        void ThrowForFullQueue()
        {
            throw new InvalidOperationException("Queue is full.");
        }
    }
}
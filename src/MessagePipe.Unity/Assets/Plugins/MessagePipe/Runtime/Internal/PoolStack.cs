using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MessagePipe.Internal
{
    internal interface IPoolStackNode<T>
        where T : class
    {
        ref T NextNode { get; }
    }

    // mutable struct, don't mark readonly.
    [StructLayout(LayoutKind.Auto)]
    internal struct PoolStack<T>
        where T : class, IPoolStackNode<T>
    {
        int gate;
        int size;
        T root;

        public int Size => size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T result)
        {
            if (Interlocked.CompareExchange(ref gate, 1, 0) == 0)
            {
                var v = root;
                if (!(v is null))
                {
                    ref var nextNode = ref v.NextNode;
                    root = nextNode;
                    nextNode = null;
                    size--;
                    result = v;
                    Volatile.Write(ref gate, 0);
                    return true;
                }

                Volatile.Write(ref gate, 0);
            }
            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPush(T item)
        {
            if (Interlocked.CompareExchange(ref gate, 1, 0) == 0)
            {
                item.NextNode = root;
                root = item;
                size++;
                Volatile.Write(ref gate, 0);
                return true;
            }
            return false;
        }
    }
}

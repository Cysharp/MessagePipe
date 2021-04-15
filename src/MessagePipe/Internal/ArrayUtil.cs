using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePipe.Internal
{
    internal static class ArrayUtil
    {
        public static T[] ImmutableAdd<T>(T[] source, T item)
        {
            var dest = new T[source.Length + 1];
            Array.Copy(source, 0, dest, 0, source.Length);
            dest[dest.Length - 1] = item;
            return dest;
        }

        public static T[] ImmutableRemove<T>(T[] source, Predicate<T> match)
        {
            if (source.Length == 0) return source;

            var index = Array.FindIndex(source, match);
            if (index == -1)
            {
                return source;
            }

            if (source.Length == 1)
            {
                return Array.Empty<T>();
            }

            var dest = new T[source.Length - 1];

            if (index == 0)
            {
                // copy [1, last]
                Array.Copy(source, 1, dest, 0, source.Length);
            }
            else if (index == source.Length - 1)
            {
                // copy [0, last-1]
                Array.Copy(source, 0, dest, 0, source.Length - 1);
            }
            else
            {
                // copy [0, index-1], [index+1-last]
                Array.Copy(source, 0, dest, 0, index - 1);
                Array.Copy(source, index, dest, index, source.Length - index);
            }

            return dest;
        }
    }
}
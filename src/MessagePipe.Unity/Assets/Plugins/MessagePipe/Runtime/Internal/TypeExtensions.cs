using System;
using System.Collections.Generic;

namespace MessagePipe.Internal
{
    internal static class TypeExtensions
    {
        public static IEnumerable<Type> GetBaseTypes(this Type t)
        {
            if (t == null) yield break;
            t = t.BaseType;
            while (t != null)
            {
                yield return t;
                t = t.BaseType;
            }
        }
    }
}

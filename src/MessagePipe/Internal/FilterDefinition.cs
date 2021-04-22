using System;
using System.Linq;
using MessagePipe.Internal;

namespace MessagePipe.Internal
{
    internal class FilterDefinition
    {
        public Type FilterType { get; }
        public int Order { get; }
        public bool IsOpenGenerics { get; }

        public Type? MessageType { get; }
        public Type? RequestType { get; }
        public Type? ResponseType { get; }

        // from AttributeFilterProvider, always closed generics.
        public FilterDefinition(Type filterType, int order)
        {
            this.FilterType = filterType;
            this.Order = order;
            this.IsOpenGenerics = false;
        }

        // set <T> type for filtering global filter
        public FilterDefinition(Type filterType, int order, Type interfaceGenericDefinition)
        {
            FilterType = filterType;
            Order = order;
            if (filterType.IsGenericType && !filterType.IsConstructedGenericType)
            {
                IsOpenGenerics = true;
            }
            else
            {
                IsOpenGenerics = false;
                var interfaceType = filterType.GetBaseTypes().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == interfaceGenericDefinition);

                var genArgs = interfaceType.GetGenericArguments();
                if (genArgs.Length == 1)
                {
                    MessageType = genArgs[0];
                }
                else
                {
                    RequestType = genArgs[0];
                    ResponseType = genArgs[1];
                }
            }
        }
    }
}
using System;
using System.Linq;

namespace MessagePipe.Internal
{
    internal abstract class FilterDefinition
    {
        public Type FilterType { get; }
        public int Order { get; }

        public FilterDefinition(Type filterType, int order)
        {
            this.FilterType = filterType;
            this.Order = order;
        }
    }

    internal sealed class AttributeFilterDefinition : FilterDefinition
    {
        public AttributeFilterDefinition(Type filterType, int order)
            : base(filterType, order)
        {
        }
    }

    internal sealed class MessageHandlerFilterDefinition : FilterDefinition
    {
        public Type MessageType { get; }
        public bool IsOpenGenerics { get; }

        public MessageHandlerFilterDefinition(Type filterType, int order, Type interfaceGenericDefinition)
            : base(filterType, order)
        {
            if (filterType.IsGenericType && !filterType.IsConstructedGenericType)
            {
                this.IsOpenGenerics = true;
                this.MessageType = null;
            }
            else
            {
                this.IsOpenGenerics = false;
                var interfaceType = filterType.GetBaseTypes().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == interfaceGenericDefinition);

                var genArgs = interfaceType.GetGenericArguments();
                this.MessageType = genArgs[0];
            }
        }
    }

    internal sealed class RequestHandlerFilterDefinition : FilterDefinition
    {
        public Type RequestType { get; }
        public Type ResponseType { get; }
        public bool IsOpenGenerics { get; }

        public RequestHandlerFilterDefinition(Type filterType, int order, Type interfaceGenericDefinition)
            : base(filterType, order)
        {
            if (filterType.IsGenericType && !filterType.IsConstructedGenericType)
            {
                this.IsOpenGenerics = true;
                this.RequestType = null;
                this.ResponseType = null;
            }
            else
            {
                this.IsOpenGenerics = false;
                var interfaceType = filterType.GetBaseTypes().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == interfaceGenericDefinition);

                var genArgs = interfaceType.GetGenericArguments();
                this.RequestType = genArgs[0];
                this.ResponseType = genArgs[1];
            }
        }
    }
}
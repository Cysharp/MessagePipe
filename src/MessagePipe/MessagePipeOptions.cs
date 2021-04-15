using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePipe
{
    public sealed class MessagePipeOptions
    {
        public MessageHandlerFilter[] GlobalMessageHandlerFilters { get; set; }

        public MessagePipeOptions()
        {
            GlobalMessageHandlerFilters = Array.Empty<MessageHandlerFilter>();
        }
    }
}

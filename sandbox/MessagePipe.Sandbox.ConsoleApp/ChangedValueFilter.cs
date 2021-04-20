using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe.Sandbox.ConsoleApp
{
    public class ChangedValueFilter<TMessage> : MessageHandlerFilter<TMessage>
    {
        TMessage lastValue;

        public override void Handle(TMessage message, Action<TMessage> next)
        {
            if (EqualityComparer<TMessage>.Default.Equals(message, lastValue))
            {
                return;
            }

            lastValue = message;
            next(message);
        }
    }
}
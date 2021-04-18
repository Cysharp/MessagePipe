using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe.Sandbox.ConsoleApp
{
    // this filter must not register as Singleton, Transient or instantiate directly.
    // T always(should) equals TMessage.
    public class ChangedValueFilter<T> : MessageHandlerFilter
    {
        T lastValue;

        public override void Handle<TMessage>(TMessage message, Action<TMessage> next)
        {
            ref var value = ref Unsafe.As<TMessage, T>(ref message);

            if (EqualityComparer<T>.Default.Equals(value, lastValue))
            {
                return;
            }

            lastValue = Unsafe.As<TMessage, T>(ref message);
            next(message);
        }
    }
}
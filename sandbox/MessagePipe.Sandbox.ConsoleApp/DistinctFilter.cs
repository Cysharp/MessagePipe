using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MessagePipe.Sandbox.ConsoleApp
{
    // this filter must not register as Singleton, Transient or instantiate directly.
    public class IntDistinctFilter : MessageHandlerFilter
    {
        int before;

        public override void Handle<T>(T message, Action<T> next)
        {
            if (before == Unsafe.As<T, int>(ref message))
            {
                return; // do nothing.
            }
            before = Unsafe.As<T, int>(ref message);
            next(message);
        }
    }
}

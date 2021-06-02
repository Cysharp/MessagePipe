#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using Microsoft.Extensions.Logging;
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


    public class LockFilter<T> : MessageHandlerFilter<T>
    {
        readonly object gate = new object();

        public override void Handle(T message, Action<T> next)
        {
            lock (gate)
            {
                next(message);
            }
        }
    }

    public class IgnoreErrorFilter<T> : MessageHandlerFilter<T>
    {
        readonly ILogger<IgnoreErrorFilter<T>> logger;

        public IgnoreErrorFilter(ILogger<IgnoreErrorFilter<T>> logger)
        {
            this.logger = logger;
        }

        public override void Handle(T message, Action<T> next)
        {
            try
            {
                next(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ""); // error logged, but do not propagate
            }
        }
    }



    public class DelayFilter<T> : AsyncMessageHandlerFilter<T>
    {
        readonly TimeSpan delaySpan;

        public DelayFilter(TimeSpan delaySpan)
        {
            this.delaySpan = delaySpan;
        }

        public override async ValueTask HandleAsync(T message, CancellationToken cancellationToken, Func<T, CancellationToken, ValueTask> next)
        {
            await Task.Delay(delaySpan, cancellationToken);
            await next(message, cancellationToken);
        }
    }
}
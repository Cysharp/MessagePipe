using MessagePipe.Internal;
using System;
using System.Runtime.CompilerServices;

namespace MessagePipe
{
    [Preserve]
    public class MessageBroker<TMessage> : IPublisher<TMessage>, ISubscriber<TMessage>
    {
        readonly MessageBrokerCore<TMessage> core;
        readonly FilterAttachedMessageHandlerFactory handlerFactory;

        [Preserve]
        public MessageBroker(MessageBrokerCore<TMessage> core, FilterAttachedMessageHandlerFactory handlerFactory)
        {
            this.core = core;
            this.handlerFactory = handlerFactory;
        }

        public void Publish(TMessage message)
        {
            core.Publish(message);
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters)
        {
            return core.Subscribe(handlerFactory.CreateMessageHandler(handler, filters));
        }
    }

    [Preserve]
    public class MessageBrokerCore<TMessage> : IDisposable, IHandlerHolderMarker
    {
        readonly FreeList<IMessageHandler<TMessage>> handlers;
        readonly MessagePipeDiagnosticsInfo diagnostics;
        readonly HandlingSubscribeDisposedPolicy handlingSubscribeDisposedPolicy;
        readonly object gate = new object();
        bool isDisposed;

        [Preserve]
        public MessageBrokerCore(MessagePipeDiagnosticsInfo diagnostics, MessagePipeOptions options)
        {
            this.handlers = new FreeList<IMessageHandler<TMessage>>();
            this.handlingSubscribeDisposedPolicy = options.HandlingSubscribeDisposedPolicy;
            this.diagnostics = diagnostics;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Publish(TMessage message)
        {
            var array = handlers.GetValues();
            for (int i = 0; i < array.Length; i++)
            {
                array[i]?.Handle(message);
            }
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler)
        {
            lock (gate)
            {
                if (isDisposed) return handlingSubscribeDisposedPolicy.Handle(nameof(MessageBrokerCore<TMessage>));

                var subscriptionKey = handlers.Add(handler);
                var subscription = new Subscription(this, subscriptionKey);
                diagnostics.IncrementSubscribe(this, subscription);
                return subscription;
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                // Dispose is called when scope is finished.
                if (!isDisposed && handlers.TryDispose(out var count))
                {
                    isDisposed = true;
                    diagnostics.RemoveTargetDiagnostics(this, count);
                }
            }
        }

        sealed class Subscription : IDisposable
        {
            bool isDisposed;
            readonly MessageBrokerCore<TMessage> core;
            readonly int subscriptionKey;

            public Subscription(MessageBrokerCore<TMessage> core, int subscriptionKey)
            {
                this.core = core;
                this.subscriptionKey = subscriptionKey;
            }

            public void Dispose()
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    lock (core.gate)
                    {
                        if (!core.isDisposed)
                        {
                            core.handlers.Remove(subscriptionKey, true);
                            core.diagnostics.DecrementSubscribe(core, this);
                        }
                    }
                }
            }
        }
    }

    [Preserve]
    public sealed class BufferedMessageBroker<TMessage> : IBufferedPublisher<TMessage>, IBufferedSubscriber<TMessage>
    {
        readonly BufferedMessageBrokerCore<TMessage> core;
        readonly FilterAttachedMessageHandlerFactory handlerFactory;

        [Preserve]
        public BufferedMessageBroker(BufferedMessageBrokerCore<TMessage> core, FilterAttachedMessageHandlerFactory handlerFactory)
        {
            this.core = core;
            this.handlerFactory = handlerFactory;
        }

        public void Publish(TMessage message)
        {
            core.Publish(message);
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler, params MessageHandlerFilter<TMessage>[] filters)
        {
            return core.Subscribe(handlerFactory.CreateMessageHandler(handler, filters));
        }
    }

    [Preserve]
    public sealed class BufferedMessageBrokerCore<TMessage>
    {
        static readonly bool IsValueType = typeof(TMessage).IsValueType;

        readonly MessageBrokerCore<TMessage> core;
        TMessage lastMessage;

        [Preserve]
        public BufferedMessageBrokerCore(MessageBrokerCore<TMessage> core)
        {
            this.core = core;
            this.lastMessage = default;
        }

        public void Publish(TMessage message)
        {
            lastMessage = message;
            core.Publish(message);
        }

        public IDisposable Subscribe(IMessageHandler<TMessage> handler)
        {
            if (IsValueType || lastMessage != null)
            {
                handler.Handle(lastMessage);
            }
            return core.Subscribe(handler);
        }
    }

    // Singleton, Scoped variation

    [Preserve]
    public class SingletonMessageBroker<TMessage> : MessageBroker<TMessage>, ISingletonPublisher<TMessage>, ISingletonSubscriber<TMessage>
    {
        [Preserve]
        public SingletonMessageBroker(SingletonMessageBrokerCore<TMessage> core, FilterAttachedMessageHandlerFactory handlerFactory)
            : base(core, handlerFactory)
        {
        }
    }

    [Preserve]
    public class ScopedMessageBroker<TMessage> : MessageBroker<TMessage>, IScopedPublisher<TMessage>, IScopedSubscriber<TMessage>
    {
        [Preserve]
        public ScopedMessageBroker(ScopedMessageBrokerCore<TMessage> core, FilterAttachedMessageHandlerFactory handlerFactory)
            : base(core, handlerFactory)
        {
        }
    }

    [Preserve]
    public class SingletonMessageBrokerCore<TMessage> : MessageBrokerCore<TMessage>
    {
        [Preserve]
        public SingletonMessageBrokerCore(MessagePipeDiagnosticsInfo diagnostics, MessagePipeOptions options)
            : base(diagnostics, options)
        {
        }
    }

    [Preserve]
    public class ScopedMessageBrokerCore<TMessage> : MessageBrokerCore<TMessage>
    {
        [Preserve]
        public ScopedMessageBrokerCore(MessagePipeDiagnosticsInfo diagnostics, MessagePipeOptions options)
            : base(diagnostics, options)
        {
        }
    }
}
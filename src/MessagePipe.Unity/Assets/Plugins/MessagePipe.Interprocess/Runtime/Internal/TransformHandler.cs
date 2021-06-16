using MessagePack;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MessagePipe.Interprocess.Internal
{
    internal sealed class TransformSyncMessageHandler<TMessage> : IAsyncMessageHandler<IInterprocessValue>
    {
        readonly IMessageHandler<TMessage> handler;
        readonly MessagePackSerializerOptions options;

        public TransformSyncMessageHandler(IMessageHandler<TMessage> handler, MessagePackSerializerOptions options)
        {
            this.handler = handler;
            this.options = options;
        }

        public UniTask HandleAsync(IInterprocessValue message, CancellationToken cancellationToken)
        {
            var msg = MessagePackSerializer.Deserialize<TMessage>(message.ValueMemory, options);
            handler.Handle(msg);
            return default;
        }
    }

    internal sealed class TransformAsyncMessageHandler<TMessage> : IAsyncMessageHandler<IInterprocessValue>
    {
        readonly IAsyncMessageHandler<TMessage> handler;
        readonly MessagePackSerializerOptions options;

        public TransformAsyncMessageHandler(IAsyncMessageHandler<TMessage> handler, MessagePackSerializerOptions options)
        {
            this.handler = handler;
            this.options = options;
        }

        public UniTask HandleAsync(IInterprocessValue message, CancellationToken cancellationToken)
        {
            var msg = MessagePackSerializer.Deserialize<TMessage>(message.ValueMemory, options);
            return handler.HandleAsync(msg, cancellationToken);
        }
    }

}

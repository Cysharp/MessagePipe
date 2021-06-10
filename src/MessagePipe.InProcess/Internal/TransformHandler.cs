using MessagePack;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe.InProcess.Internal
{
    internal sealed class TransformSyncMessageHandler<TMessage> : IAsyncMessageHandler<IInProcessValue>
    {
        readonly IMessageHandler<TMessage> handler;
        readonly MessagePackSerializerOptions options;

        public TransformSyncMessageHandler(IMessageHandler<TMessage> handler, MessagePackSerializerOptions options)
        {
            this.handler = handler;
            this.options = options;
        }

        public ValueTask HandleAsync(IInProcessValue message, CancellationToken cancellationToken)
        {
            var msg = MessagePackSerializer.Deserialize<TMessage>(message.ValueMemory, options);
            handler.Handle(msg);
            return default;
        }
    }

    internal sealed class TransformAsyncMessageHandler<TMessage> : IAsyncMessageHandler<IInProcessValue>
    {
        readonly IAsyncMessageHandler<TMessage> handler;
        readonly MessagePackSerializerOptions options;

        public TransformAsyncMessageHandler(IAsyncMessageHandler<TMessage> handler, MessagePackSerializerOptions options)
        {
            this.handler = handler;
            this.options = options;
        }

        public ValueTask HandleAsync(IInProcessValue message, CancellationToken cancellationToken)
        {
            var msg = MessagePackSerializer.Deserialize<TMessage>(message.ValueMemory, options);
            return handler.HandleAsync(msg, cancellationToken);
        }
    }

}

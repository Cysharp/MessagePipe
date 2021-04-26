namespace MessagePipe
{
    public sealed class EventFactory
    {
        readonly MessagePipeOptions options;
        readonly MessagePipeDiagnosticsInfo diagnosticsInfo;
        readonly FilterAttachedMessageHandlerFactory handlerFactory;

        public EventFactory(MessagePipeOptions options, MessagePipeDiagnosticsInfo diagnosticsInfo, FilterAttachedMessageHandlerFactory handlerFactory)
        {
            this.options = options;
            this.diagnosticsInfo = diagnosticsInfo;
            this.handlerFactory = handlerFactory;
        }

        public (IPublisher<T>, ISubscriber<T>) Create<T>()
        {
            var core = new MessageBrokerCore<T>(diagnosticsInfo, options);
            var broker = new MessageBroker<T>(core, handlerFactory);
            return (broker, broker);
        }
    }
}
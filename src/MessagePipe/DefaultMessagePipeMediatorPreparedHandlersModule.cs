namespace MessagePipe;

public abstract class DefaultMessagePipeMediatorPreparedHandlersModule : IMessagePipeMediatorPreparedHandlersModule
{
    public IMessagePipeMediator Mediator { get; set; }


    public DefaultMessagePipeMediatorPreparedHandlersModule(
        IMessagePipeMediator mediator)
    {
        Mediator = mediator;

        this.PrepareAllHandlers();
    }
    
    public abstract void PrepareAllHandlers();
}
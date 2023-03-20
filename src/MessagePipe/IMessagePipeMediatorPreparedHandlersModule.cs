namespace MessagePipe;

public interface IMessagePipeMediatorPreparedHandlersModule
{
    IMessagePipeMediator Mediator { get; set; }
    
    void PrepareAllHandlers();
}
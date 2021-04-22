using MessagePipe;
using System;
using VContainer;

public static class TestHelper
{
    public static IObjectResolver BuildContainer(Action<MessagePipeOptions> configure, Action<MessagePipeOptions, ContainerBuilder> use)
    {
        var builder = new ContainerBuilder();
        var options = builder.RegisterMessagePipe(configure);
        use(options, builder);

        builder.RegisterContainer(); // for unittest purpose
        return builder.Build();
    }

    public static IObjectResolver BuildContainer(Action<MessagePipeOptions, ContainerBuilder> use)
    {
        var builder = new ContainerBuilder();
        var options = builder.RegisterMessagePipe();
        use(options, builder);

        builder.RegisterContainer(); // for unittest purpose
        return builder.Build();
    }
}

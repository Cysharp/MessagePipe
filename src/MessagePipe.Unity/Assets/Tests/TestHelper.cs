﻿using MessagePipe;
using System;
using VContainer;
using Zenject;

public static class TestHelper
{
    public static IObjectResolver BuildVContainer()
    {
        var builder = new ContainerBuilder();
        builder.RegisterMessagePipe();
        return builder.Build();
    }
    
    public static IObjectResolver BuildVContainer(Action<MessagePipeOptions> configure, Action<MessagePipeOptions, ContainerBuilder> use)
    {
        var builder = new ContainerBuilder();
        var options = builder.RegisterMessagePipe(configure);
        use(options, builder);

        return builder.Build();
    }

    public static IObjectResolver BuildVContainer(Action<MessagePipeOptions, ContainerBuilder> use)
    {
        var builder = new ContainerBuilder();
        var options = builder.RegisterMessagePipe();
        use(options, builder);

        return builder.Build();
    }

    public static DiContainer BuildZenject(Action<MessagePipeOptions> configure, Action<MessagePipeOptions, DiContainer> use)
    {
        var builder = new DiContainer();
        var options = builder.BindMessagePipe(configure);
        use(options, builder);

        return builder;
    }

    public static DiContainer BuildZenject(Action<MessagePipeOptions, DiContainer> use)
    {
        var builder = new DiContainer();
        var options = builder.BindMessagePipe();
        use(options, builder);

        return builder;
    }

    public static IServiceProvider BuildBuiltin(Action<MessagePipeOptions> configure, Action<BuiltinContainerBuilder> use)
    {
        var builder = new BuiltinContainerBuilder();
        builder.AddMessagePipe(configure);
        use(builder);

        return builder.BuildServiceProvider();
    }

    public static IServiceProvider BuildBuiltin(Action<BuiltinContainerBuilder> use)
    {
        var builder = new BuiltinContainerBuilder();
        builder.AddMessagePipe();
        use(builder);

        return builder.BuildServiceProvider();
    }
}

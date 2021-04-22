using MessagePipe;
using NUnit.Framework;
using System;
using UnityEngine;
using VContainer;

public class VContainerTest
{
    [Test]
    public void VContainerTestSimplePasses()
    {
        var builder = new ContainerBuilder();
        var options = builder.RegisterMessagePipe();

        builder.Register<IServiceProvider, UnitTestServiceProviderProxy>(Lifetime.Singleton);

        // others
        builder.RegisterMessageBroker<int>(options);

        // add
        

        var resolver = builder.Build();
        var proxy = resolver.Resolve<IServiceProvider>() as UnitTestServiceProviderProxy;
        proxy.SetResolver(resolver);
        
        
        builder.RegisterMessageBroker<int>(options);

        var pubInt = resolver.Resolve<IPublisher<int>>();
        var subInt = resolver.Resolve<ISubscriber<int>>();

        subInt.Subscribe(x => Debug.Log(x));

        pubInt.Publish(100);
    }
}

public sealed class UnitTestServiceProviderProxy : IServiceProvider
{
    IObjectResolver resolver;

    // require to hack VContainer's bug.
    public void SetResolver(IObjectResolver resolver)
    {
        this.resolver = resolver;
    }

    public object GetService(Type serviceType)
    {
        return resolver.Resolve(serviceType);
    }

}

public class MyClass
{
    readonly IServiceProvider resolver;
    public MyClass(IServiceProvider resolver)
    {
        this.resolver = resolver;
    }
}

using MessagePipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DataStore
{
    public List<string> Logs { get; set; }

    public DataStore()
    {
        Logs = new List<string>();
    }
}


public class MyFilter<T> : MessageHandlerFilter<T>
{
    readonly DataStore store;

    public MyFilter(DataStore store)
    {
        this.store = store;
    }

    public override void Handle(T message, Action<T> next)
    {
        store.Logs.Add($"Order:{Order}");
        next(message);
    }
}

[MessageHandlerFilter(typeof(MyFilter<int>), Order = 999)]
[MessageHandlerFilter(typeof(MyFilter<int>), Order = 1099)]
public class MyHandler : IMessageHandler<int>
{
    DataStore store;

    public MyHandler(DataStore store)
    {
        this.store = store;
    }

    public void Handle(int message)
    {
        store.Logs.Add("Handle:" + message);
    }
}

[RequestHandlerFilter(typeof(MyRequestHandlerFilter), Order = 999)]
[RequestHandlerFilter(typeof(MyRequestHandlerFilter), Order = 1099)]
public class MyRequestHandler : IRequestHandler<int, int>
{
    readonly DataStore store;

    public MyRequestHandler(DataStore store)
    {
        this.store = store;
    }

    public int Invoke(int request)
    {
        store.Logs.Add("Invoke:" + request);
        return request * 10;
    }
}

public class MyRequestHandler2 : IRequestHandler<int, int>
{
    readonly DataStore store;

    public MyRequestHandler2(DataStore store)
    {
        this.store = store;
    }

    public int Invoke(int request)
    {
        store.Logs.Add("Invoke2:" + request);
        return request * 100;
    }
}

public class MyRequestHandlerFilter : RequestHandlerFilter<int, int>
{
    readonly DataStore store;

    public MyRequestHandlerFilter(DataStore store)
    {
        this.store = store;
    }

    public override int Invoke(int request, Func<int, int> next)
    {
        store.Logs.Add($"Order:{Order}");
        return next(request);
    }
}

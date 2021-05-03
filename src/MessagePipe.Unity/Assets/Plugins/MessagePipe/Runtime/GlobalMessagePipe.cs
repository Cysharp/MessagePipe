#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604

#if !UNITY_2018_3_OR_NEWER
using Microsoft.Extensions.DependencyInjection;
#endif
using System;

namespace MessagePipe
{
    public static class GlobalMessagePipe
    {
        static IServiceProvider provider;
        static EventFactory eventFactory;
        static MessagePipeDiagnosticsInfo diagnosticsInfo;

        public static void SetProvider(IServiceProvider provider)
        {
            GlobalMessagePipe.provider = provider;
            GlobalMessagePipe.eventFactory = provider.GetRequiredService<EventFactory>();
            GlobalMessagePipe.diagnosticsInfo = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
        }

        public static bool IsInitialized => provider != null;

        public static MessagePipeDiagnosticsInfo DiagnosticsInfo
        {
            get
            {
                ThrowIfNotInitialized();
                return diagnosticsInfo;
            }
        }

        public static IPublisher<TMessage> GetPublisher<TMessage>()
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IPublisher<TMessage>>();
        }

        public static ISubscriber<TMessage> GetSubscriber<TMessage>()
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<ISubscriber<TMessage>>();
        }

        public static IAsyncPublisher<TMessage> GetAsyncPublisher<TMessage>()
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IAsyncPublisher<TMessage>>();
        }

        public static IAsyncSubscriber<TMessage> GetAsyncSubscriber<TMessage>()
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IAsyncSubscriber<TMessage>>();
        }

        public static IPublisher<TKey, TMessage> GetPublisher<TKey, TMessage>()
            
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IPublisher<TKey, TMessage>>();
        }

        public static ISubscriber<TKey, TMessage> GetSubscriber<TKey, TMessage>()
            
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<ISubscriber<TKey, TMessage>>();
        }

        public static IAsyncPublisher<TKey, TMessage> GetAsyncPublisher<TKey, TMessage>()
            
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IAsyncPublisher<TKey, TMessage>>();
        }

        public static IAsyncSubscriber<TKey, TMessage> GetAsyncSubscriber<TKey, TMessage>()
            
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IAsyncSubscriber<TKey, TMessage>>();
        }

        public static IRequestHandler<TRequest, TResponse> GetRequestHandler<TRequest, TResponse>()
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        }

        public static IAsyncRequestHandler<TRequest, TResponse> GetAsyncRequestHandler<TRequest, TResponse>()
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IAsyncRequestHandler<TRequest, TResponse>>();
        }

        public static IRequestAllHandler<TRequest, TResponse> GetRequestAllHandler<TRequest, TResponse>()
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IRequestAllHandler<TRequest, TResponse>>();
        }

        public static IAsyncRequestAllHandler<TRequest, TResponse> GetAsyncRequestAllHandler<TRequest, TResponse>()
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IAsyncRequestAllHandler<TRequest, TResponse>>();
        }

#if !UNITY_2018_3_OR_NEWER

        public static IDistributedPublisher<TKey, TMessage> GetDistributedPublisher<TKey, TMessage>()
            
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IDistributedPublisher<TKey, TMessage>>();
        }

        public static IDistributedSubscriber<TKey, TMessage> GetDistributedSubscriber<TKey, TMessage>()
            
        {
            ThrowIfNotInitialized();
            return provider.GetRequiredService<IDistributedSubscriber<TKey, TMessage>>();
        }

#endif

        public static (IDisposablePublisher<T>, ISubscriber<T>) CreateEvent<T>()
        {
            ThrowIfNotInitialized();
            return eventFactory.CreateEvent<T>();
        }

        public static (IDisposableAsyncPublisher<T>, IAsyncSubscriber<T>) CreateAsyncEvent<T>()
        {
            ThrowIfNotInitialized();
            return eventFactory.CreateAsyncEvent<T>();
        }

        public static (IDisposableBufferedPublisher<T>, IBufferedSubscriber<T>) CreateBufferedEvent<T>(T initialValue)
        {
            ThrowIfNotInitialized();
            return eventFactory.CreateBufferedEvent<T>(initialValue);
        }

        public static (IDisposableBufferedAsyncPublisher<T>, IBufferedAsyncSubscriber<T>) CreateBufferedAsyncEvent<T>(T initialValue)
        {
            ThrowIfNotInitialized();
            return eventFactory.CreateBufferedAsyncEvent<T>(initialValue);
        }

        // [MemberNotNull(nameof(provider), nameof(eventFactory), nameof(diagnosticsInfo))]
        static void ThrowIfNotInitialized()
        {
            if (provider == null || eventFactory == null || diagnosticsInfo == null)
            {
                throw new InvalidOperationException("Require to call `SetProvider` before use GlobalMessagePipe.");
            }
        }
    }
}

using System;
using MessagePipe.Internal;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace MessagePipe
{
    public static partial class SubscriberExtensions
    {
        public static UniTask<TMessage> FirstAsync<TMessage>(this ISubscriber<TMessage> subscriber, CancellationToken cancellationToken, params MessageHandlerFilter<TMessage>[] filters)
        {
            return new UniTask<TMessage>(new FirstAsyncMessageHandler<TMessage>(subscriber, cancellationToken, filters), 0);
        }

        public static UniTask<TMessage> FirstAsync<TMessage>(this ISubscriber<TMessage> subscriber, CancellationToken cancellationToken, Func<TMessage, bool> predicate, params MessageHandlerFilter<TMessage>[] filters)
        {
            var predicateFilter = new PredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return new UniTask<TMessage>(new FirstAsyncMessageHandler<TMessage>(subscriber, cancellationToken, filters), 0);
        }
        public static UniTask<TMessage> FirstAsync<TMessage>(this IBufferedSubscriber<TMessage> subscriber, CancellationToken cancellationToken, params MessageHandlerFilter<TMessage>[] filters)
        {
            return new UniTask<TMessage>(new FirstAsyncBufferedMessageHandler<TMessage>(subscriber, cancellationToken, filters), 0);
        }

        public static UniTask<TMessage> FirstAsync<TMessage>(this IBufferedSubscriber<TMessage> subscriber, CancellationToken cancellationToken, Func<TMessage, bool> predicate, params MessageHandlerFilter<TMessage>[] filters)
        {
            var predicateFilter = new PredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return new UniTask<TMessage>(new FirstAsyncBufferedMessageHandler<TMessage>(subscriber, cancellationToken, filters), 0);
        }

        public static UniTask<TMessage> FirstAsync<TMessage>(this IAsyncSubscriber<TMessage> subscriber, CancellationToken cancellationToken, params AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            return new UniTask<TMessage>(new FirstAsyncAsyncMessageHandler<TMessage>(subscriber, cancellationToken, filters), 0);
        }

        public static UniTask<TMessage> FirstAsync<TMessage>(this IAsyncSubscriber<TMessage> subscriber, CancellationToken cancellationToken, Func<TMessage, bool> predicate, params AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            var predicateFilter = new AsyncPredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return new UniTask<TMessage>(new FirstAsyncAsyncMessageHandler<TMessage>(subscriber, cancellationToken, filters), 0);
        }

        public static async UniTask<TMessage> FirstAsync<TMessage>(this IBufferedAsyncSubscriber<TMessage> subscriber, CancellationToken cancellationToken, params AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            return await new UniTask<TMessage>(await FirstAsyncAsyncBufferedMessageHandler<TMessage>.CreateAsync(subscriber, cancellationToken, filters), 0);
        }

        public static async UniTask<TMessage> FirstAsync<TMessage>(this IBufferedAsyncSubscriber<TMessage> subscriber, CancellationToken cancellationToken, Func<TMessage, bool> predicate, params AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            var predicateFilter = new AsyncPredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return await new UniTask<TMessage>(await FirstAsyncAsyncBufferedMessageHandler<TMessage>.CreateAsync(subscriber, cancellationToken, filters), 0);
        }
        public static UniTask<TMessage> FirstAsync<TKey, TMessage>(this ISubscriber<TKey, TMessage> subscriber, TKey key, CancellationToken cancellationToken, params MessageHandlerFilter<TMessage>[] filters)
            
        {
            return new UniTask<TMessage>(new FirstAsyncMessageHandler<TKey, TMessage>(subscriber, key, cancellationToken, filters), 0);
        }

        public static UniTask<TMessage> FirstAsync<TKey, TMessage>(this ISubscriber<TKey, TMessage> subscriber, TKey key, CancellationToken cancellationToken, Func<TMessage, bool> predicate, params MessageHandlerFilter<TMessage>[] filters)
            
        {
            var predicateFilter = new PredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return new UniTask<TMessage>(new FirstAsyncMessageHandler<TKey, TMessage>(subscriber, key, cancellationToken, filters), 0);
        }

        public static UniTask<TMessage> FirstAsync<TKey, TMessage>(this IAsyncSubscriber<TKey, TMessage> subscriber, TKey key, CancellationToken cancellationToken, params AsyncMessageHandlerFilter<TMessage>[] filters)
            
        {
            return new UniTask<TMessage>(new FirstAsyncAsyncMessageHandler<TKey, TMessage>(subscriber, key, cancellationToken, filters), 0);
        }

        public static UniTask<TMessage> FirstAsync<TKey, TMessage>(this IAsyncSubscriber<TKey, TMessage> subscriber, TKey key, CancellationToken cancellationToken, Func<TMessage, bool> predicate, params AsyncMessageHandlerFilter<TMessage>[] filters)
            
        {
            var predicateFilter = new AsyncPredicateFilter<TMessage>(predicate);
            filters = (filters.Length == 0)
                ? new[] { predicateFilter }
                : ArrayUtil.ImmutableAdd(filters, predicateFilter);

            return new UniTask<TMessage>(new FirstAsyncAsyncMessageHandler<TKey, TMessage>(subscriber, key, cancellationToken, filters), 0);
        }
    }

    internal sealed class FirstAsyncMessageHandler<TKey, TMessage> : IMessageHandler<TMessage>, IUniTaskSource<TMessage>
        
    {
        int handleCalled = 0;
        IDisposable subscription;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        UniTaskCompletionSourceCore<TMessage> core;

        static readonly Action<object> cancelCallback = Cancel;

        public FirstAsyncMessageHandler(ISubscriber<TKey, TMessage> subscriber, TKey key, CancellationToken cancellationToken, MessageHandlerFilter<TMessage>[] filters)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                this.core.TrySetException(new OperationCanceledException(cancellationToken));
                return;
            }

            try
            {
                this.subscription = subscriber.Subscribe(key, this, filters);
            }
            catch (Exception ex)
            {
                this.core.TrySetException(ex);
                return;
            }

            if (handleCalled != 0)
            {
                this.subscription?.Dispose();
                return;
            }

            if (cancellationToken.CanBeCanceled)
            {
                this.cancellationToken = cancellationToken;
                this.cancellationTokenRegistration = cancellationToken.Register(cancelCallback, this, false);
            }
        }

        static void Cancel(object state)
        {
            var self = (FirstAsyncMessageHandler<TKey, TMessage>)state;
            self.subscription?.Dispose();
            self.core.TrySetException(new OperationCanceledException(self.cancellationToken));
        }

        public void Handle(TMessage message)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    core.TrySetResult(message);
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }
        }

        void IUniTaskSource.GetResult(short token) => GetResult(token);
        public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();
        public /*replaced*/ UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public TMessage GetResult(short token)
        {
            return core.GetResult(token);
        }
    }
    internal sealed class FirstAsyncMessageHandler<TMessage> : IMessageHandler<TMessage>, IUniTaskSource<TMessage>
    {
        int handleCalled = 0;
        IDisposable subscription;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        UniTaskCompletionSourceCore<TMessage> core;

        static readonly Action<object> cancelCallback = Cancel;

        public FirstAsyncMessageHandler(ISubscriber<TMessage> subscriber, CancellationToken cancellationToken, MessageHandlerFilter<TMessage>[] filters)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                this.core.TrySetException(new OperationCanceledException(cancellationToken));
                return;
            }

            try
            {
                this.subscription = subscriber.Subscribe(this, filters);
            }
            catch (Exception ex)
            {
                this.core.TrySetException(ex);
                return;
            }

            if (handleCalled != 0)
            {
                this.subscription?.Dispose();
                return;
            }

            if (cancellationToken.CanBeCanceled)
            {
                this.cancellationToken = cancellationToken;
                this.cancellationTokenRegistration = cancellationToken.Register(cancelCallback, this, false);
            }
        }

        static void Cancel(object state)
        {
            var self = (FirstAsyncMessageHandler<TMessage>)state;
            self.subscription?.Dispose();
            self.core.TrySetException(new OperationCanceledException(self.cancellationToken));
        }

        public void Handle(TMessage message)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    core.TrySetResult(message);
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }
        }

        void IUniTaskSource.GetResult(short token) => GetResult(token);
        public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();
        public /*replaced*/ UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public TMessage GetResult(short token)
        {
            return core.GetResult(token);
        }
    }

    internal sealed class FirstAsyncBufferedMessageHandler<TMessage> : IMessageHandler<TMessage>, IUniTaskSource<TMessage>
    {
        int handleCalled = 0;
        IDisposable subscription;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        UniTaskCompletionSourceCore<TMessage> core;

        static readonly Action<object> cancelCallback = Cancel;

        public FirstAsyncBufferedMessageHandler(IBufferedSubscriber<TMessage> subscriber, CancellationToken cancellationToken, MessageHandlerFilter<TMessage>[] filters)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                this.core.TrySetException(new OperationCanceledException(cancellationToken));
                return;
            }

            try
            {
                this.subscription = subscriber.Subscribe(this, filters);
            }
            catch (Exception ex)
            {
                this.core.TrySetException(ex);
                return;
            }

            if (handleCalled != 0)
            {
                this.subscription?.Dispose();
                return;
            }

            if (cancellationToken.CanBeCanceled)
            {
                this.cancellationToken = cancellationToken;
                this.cancellationTokenRegistration = cancellationToken.Register(cancelCallback, this, false);
            }
        }

        static void Cancel(object state)
        {
            var self = (FirstAsyncBufferedMessageHandler<TMessage>)state;
            self.subscription?.Dispose();
            self.core.TrySetException(new OperationCanceledException(self.cancellationToken));
        }

        public void Handle(TMessage message)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    core.TrySetResult(message);
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }
        }

        void IUniTaskSource.GetResult(short token) => GetResult(token);
        public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();
        public /*replaced*/ UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public TMessage GetResult(short token)
        {
            return core.GetResult(token);
        }
    }

    internal sealed class FirstAsyncAsyncMessageHandler<TKey, TMessage> : IAsyncMessageHandler<TMessage>, IUniTaskSource<TMessage>
        
    {
        int handleCalled = 0;
        IDisposable subscription;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        UniTaskCompletionSourceCore<TMessage> core;

        static readonly Action<object> cancelCallback = Cancel;

        public FirstAsyncAsyncMessageHandler(IAsyncSubscriber<TKey, TMessage> subscriber, TKey key, CancellationToken cancellationToken, AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                this.core.TrySetException(new OperationCanceledException(cancellationToken));
                return;
            }

            try
            {
                this.subscription = subscriber.Subscribe(key, this, filters);
            }
            catch (Exception ex)
            {
                this.core.TrySetException(ex);
                return;
            }

            if (handleCalled != 0)
            {
                this.subscription?.Dispose();
                return;
            }

            if (cancellationToken.CanBeCanceled)
            {
                this.cancellationToken = cancellationToken;
                this.cancellationTokenRegistration = cancellationToken.Register(cancelCallback, this, false);
            }
        }

        static void Cancel(object state)
        {
            var self = (FirstAsyncAsyncMessageHandler<TKey, TMessage>)state;
            self.subscription?.Dispose();
            self.core.TrySetException(new OperationCanceledException(self.cancellationToken));
        }

        public UniTask HandleAsync(TMessage message, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        core.TrySetException(new OperationCanceledException(cancellationToken));
                    }
                    else
                    {
                        core.TrySetResult(message);
                    }
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }
            return default;
        }

        void IUniTaskSource.GetResult(short token) => GetResult(token);
        public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();
        public /*replaced*/ UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public TMessage GetResult(short token)
        {
            return core.GetResult(token);
        }
    }

    internal sealed class FirstAsyncAsyncMessageHandler<TMessage> : IAsyncMessageHandler<TMessage>, IUniTaskSource<TMessage>
    {
        int handleCalled = 0;
        IDisposable subscription;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        UniTaskCompletionSourceCore<TMessage> core;

        static readonly Action<object> cancelCallback = Cancel;

        public FirstAsyncAsyncMessageHandler(IAsyncSubscriber<TMessage> subscriber, CancellationToken cancellationToken, AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                this.core.TrySetException(new OperationCanceledException(cancellationToken));
                return;
            }

            try
            {
                this.subscription = subscriber.Subscribe(this, filters);
            }
            catch (Exception ex)
            {
                this.core.TrySetException(ex);
                return;
            }

            if (handleCalled != 0)
            {
                this.subscription?.Dispose();
                return;
            }

            if (cancellationToken.CanBeCanceled)
            {
                this.cancellationToken = cancellationToken;
                this.cancellationTokenRegistration = cancellationToken.Register(cancelCallback, this, false);
            }
        }

        static void Cancel(object state)
        {
            var self = (FirstAsyncAsyncMessageHandler<TMessage>)state;
            self.subscription?.Dispose();
            self.core.TrySetException(new OperationCanceledException(self.cancellationToken));
        }

        public UniTask HandleAsync(TMessage message, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        core.TrySetException(new OperationCanceledException(cancellationToken));
                    }
                    else
                    {
                        core.TrySetResult(message);
                    }
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }
            return default;
        }

        void IUniTaskSource.GetResult(short token) => GetResult(token);
        public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();
        public /*replaced*/ UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public TMessage GetResult(short token)
        {
            return core.GetResult(token);
        }
    }

    internal sealed class FirstAsyncAsyncBufferedMessageHandler<TMessage> : IAsyncMessageHandler<TMessage>, IUniTaskSource<TMessage>
    {
        int handleCalled = 0;
        IDisposable subscription;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        UniTaskCompletionSourceCore<TMessage> core;

        static readonly Action<object> cancelCallback = Cancel;

        public static async UniTask<FirstAsyncAsyncBufferedMessageHandler<TMessage>> CreateAsync(IBufferedAsyncSubscriber<TMessage> subscriber, CancellationToken cancellationToken, AsyncMessageHandlerFilter<TMessage>[] filters)
        {
            var self = new FirstAsyncAsyncBufferedMessageHandler<TMessage>();
            if (cancellationToken.IsCancellationRequested)
            {
                self.core.TrySetException(new OperationCanceledException(cancellationToken));
                return self;
            }

            try
            {
                self.subscription = await subscriber.SubscribeAsync(self, filters).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                self.core.TrySetException(ex);
                return self;
            }

            if (self.handleCalled != 0)
            {
                self.subscription?.Dispose();
                return self;
            }

            if (cancellationToken.CanBeCanceled)
            {
                self.cancellationToken = cancellationToken;
                self.cancellationTokenRegistration = cancellationToken.Register(cancelCallback, self, false);
            }
            return self;
        }

        static void Cancel(object state)
        {
            var self = (FirstAsyncAsyncBufferedMessageHandler<TMessage>)state;
            self.subscription?.Dispose();
            self.core.TrySetException(new OperationCanceledException(self.cancellationToken));
        }

        public UniTask HandleAsync(TMessage message, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref handleCalled) == 1)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        core.TrySetException(new OperationCanceledException(cancellationToken));
                    }
                    else
                    {
                        core.TrySetResult(message);
                    }
                }
                finally
                {
                    subscription?.Dispose();
                    cancellationTokenRegistration.Dispose();
                }
            }
            return default;
        }

        void IUniTaskSource.GetResult(short token) => GetResult(token);
        public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();
        public /*replaced*/ UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public TMessage GetResult(short token)
        {
            return core.GetResult(token);
        }
    }
}
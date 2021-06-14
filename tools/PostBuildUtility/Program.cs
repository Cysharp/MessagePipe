using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PostBuildUtility
{
    class Program : ConsoleAppBase
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
        }

        [Command("replace-to-unity")]
        public void ReplaceToUnity([Option(0)] string directory)
        {
            var mutex = new Mutex(false, "MessagePipe." + nameof(ReplaceToUnity));
            if (!mutex.WaitOne(0, false))
            {
                System.Console.WriteLine("running in another process, quit.");
                return; // mutex will release after quit.
            }

            var replaceSet = new Dictionary<string, string>
            {
                // to UniTask
                {", ValueTaskSourceOnCompletedFlags flags", "" },
                {"ValueTaskAwaiter<TResponse>", "Cysharp.Threading.Tasks.UniTask<TResponse>.Awaiter" },
                {"ValueTaskAwaiter", "Cysharp.Threading.Tasks.UniTask.Awaiter" },
                {"ValueTaskSourceStatus", "UniTaskStatus" },
                {"IValueTaskSource", "IUniTaskSource" },
                {"TaskCompletionSource", "UniTaskCompletionSource" },
                {"ManualResetValueTaskSourceCore", "UniTaskCompletionSourceCore" },
                {"ValueTask", "UniTask" },
                {"IAsyncDisposable", "IUniTaskAsyncDisposable" },
                {"using System.Threading.Tasks.Sources;", "" },
                {"System.Threading.Tasks", "Cysharp.Threading.Tasks" },
                {"IAsyncEnumerable", "IUniTaskAsyncEnumerable" },
                {"IAsyncEnumerator", "IUniTaskAsyncEnumerator" },
                {"[EnumeratorCancellation]", "" },
                {" core.OnCompleted(continuation, state, token, flags);", " core.OnCompleted(continuation, state, token);" },
                {"core.SetException", "core.TrySetException" },
                {"core.SetResult", "core.TrySetResult" },
                {"public UniTaskStatus GetStatus(short token)", @"void IUniTaskSource.GetResult(short token) => GetResult(token);
        public UniTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();
        public UniTaskStatus GetStatus(short token)" },

                // Remove nullable
                {"T?", "T" },
                {"T[]?", "T[]" },
                {"Assembly[]?", "Assembly[]" },
                {"Type[]?", "Type[]" },
                {"ExceptionDispatchInfo?", "ExceptionDispatchInfo" },
                {"Type?", "Type" },
                {"AsyncRequestHandlerFilter[]?", "AsyncRequestHandlerFilter[]" },
                {"RequestHandlerFilter[]?", "RequestHandlerFilter[]" },
                {"AsyncMessageHandlerFilter[]?", "AsyncMessageHandlerFilter[]" },
                {"MessageHandlerFilter[]?", "MessageHandlerFilter[]" },
                {"IAsyncMessageHandler<T>?", "IAsyncMessageHandler<T>" },
                {"IAsyncMessageHandler<TMessage>?", "IAsyncMessageHandler<TMessage>" },
                {"IMessageHandler<TMessage>?", "IMessageHandler<TMessage>" },
                {"AwaiterNode?", "AwaiterNode" },
                {"default!", "default" },
                {"null!", "null" },
                {"result!", "result" },
                {"where TKey : notnull", ""},
                {"IServiceProvider?", "IServiceProvider"},
                {"EventFactory?", "EventFactory"},
                {"MessagePipeDiagnosticsInfo?", "MessagePipeDiagnosticsInfo"},
                {"IDisposable?", "IDisposable"},
                {"TMessage?", "TMessage"},
                {"string?", "string"},
                {"lastMessage!", "lastMessage"},
                {"state!", "state"},
                {"object?", "object"},
                {"IInterprocessKey?", "IInterprocessKey"},
                {"Func<CancellationToken, Task>?", "Func<CancellationToken, System.Threading.Tasks.Task>" },
                {"(genArgs[0].FullName!, genArgs[1].FullName)!", "(genArgs[0].FullName, genArgs[1].FullName)" },
                {"(requestType.FullName!, responseType.FullName!)", "(requestType.FullName, responseType.FullName)" },

                {"task!", "task" },
                {"!.Invoke", ".Invoke" },
                {"!.GetValue", ".GetValue" },

                // final transform
                {"Cysharp.Threading.Tasks.Task", "System.Threading.Tasks.Task" }
            };

            System.Console.WriteLine("Start to replace code, remove nullability and use UniTask.");
            var noBomUtf8 = new UTF8Encoding(false);

            foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(path, Encoding.UTF8);

                foreach (var item in replaceSet)
                {
                    text = text.Replace(item.Key, item.Value);
                }

                File.WriteAllText(path, text, noBomUtf8);
            }

            System.Console.WriteLine("Replace complete.");
        }
    }
}

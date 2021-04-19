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
                {"ValueTask", "UniTask" },

                //{"Exception?", "Exception" },
                //{"Action<LogInfo, Exception>?", "Action<LogInfo, Exception>" },
                //{"Utf8JsonWriter?", "Utf8JsonWriter" },
                //{"string?", "string" },
                //{"object?", "object" },
                //{"default!", "default" },
                //{"fn!", "fn" },
                //{"byte[]?", "byte[]" },
                //{"null!", "null" },
                //{"className!", "className" },
                //{"Action<Utf8JsonWriter, LogInfo>?", "Action<Utf8JsonWriter, LogInfo>" },
                //{"Action<IBufferWriter<byte>, LogInfo>?", "Action<IBufferWriter<byte>, LogInfo>" },
                //{"Action<Exception>?", "Action<Exception>" },
                //{"Func<T, LogInfo, IZLoggerEntry>?", "Func<T, LogInfo, IZLoggerEntry>" },
                //{"ToString()!", "ToString()" },
                //{"FieldInfo?", "FieldInfo" }
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

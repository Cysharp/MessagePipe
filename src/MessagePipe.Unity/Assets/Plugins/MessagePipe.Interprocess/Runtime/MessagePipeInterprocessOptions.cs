using MessagePack;
using MessagePack.Resolvers;
using System;

namespace MessagePipe.Interprocess
{
    public sealed class MessagePipeInterprocessUdpOptions
    {
        public string Host { get; }
        public int Port { get; }
        public MessagePackSerializerOptions MessagePackSerializerOptions { get; set; }
        public InstanceLifetime InstanceLifetime { get; set; }
        public Action<string, Exception> UnhandledErrorHandler { get; set; }

        public MessagePipeInterprocessUdpOptions(string host, int port)
        {
            this.Host = host;
            this.Port = port;
            this.MessagePackSerializerOptions = ContractlessStandardResolver.Options;
            this.InstanceLifetime = InstanceLifetime.Scoped;
            this.UnhandledErrorHandler = (msg, x) => Console.WriteLine(msg + x);
        }
    }

    public sealed class MessagePipeInterprocessNamedPipeOptions
    {
        public string PipeName { get; }
        public string ServerName { get; set; }
        public MessagePackSerializerOptions MessagePackSerializerOptions { get; set; }
        public InstanceLifetime InstanceLifetime { get; set; }
        public bool? AsServer { get; set; }
        public Action<string, Exception> UnhandledErrorHandler { get; set; }

        public MessagePipeInterprocessNamedPipeOptions(string pipeName)
        {
            this.PipeName = pipeName;
            this.ServerName = ".";
            this.MessagePackSerializerOptions = ContractlessStandardResolver.Options;
            this.InstanceLifetime = InstanceLifetime.Scoped;
            this.UnhandledErrorHandler = (msg, x) => Console.WriteLine(msg + x);
            this.AsServer = null;
        }
    }

    public sealed class MessagePipeInterprocessTcpOptions
    {
        public string Host { get; }
        public int Port { get; }
        public MessagePackSerializerOptions MessagePackSerializerOptions { get; set; }
        public InstanceLifetime InstanceLifetime { get; set; }
        public bool? AsServer { get; set; }
        public Action<string, Exception> UnhandledErrorHandler { get; set; }

        public MessagePipeInterprocessTcpOptions(string host, int port)
        {
            this.Host = host;
            this.Port = port;
            this.MessagePackSerializerOptions = ContractlessStandardResolver.Options;
            this.InstanceLifetime = InstanceLifetime.Scoped;
            this.UnhandledErrorHandler = (msg, x) => Console.WriteLine(msg + x);
            this.AsServer = null;
        }
    }
}
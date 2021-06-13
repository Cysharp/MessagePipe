using MessagePack;
using MessagePack.Resolvers;
using System;

namespace MessagePipe.InProcess
{
    public sealed class MessagePipeInProcessUdpOptions
    {
        public string Host { get; }
        public int Port { get; }
        public MessagePackSerializerOptions MessagePackSerializerOptions { get; set; }
        public InstanceLifetime InstanceLifetime { get; set; }
        public Action<string, Exception> UnhandledErrorHandler { get; set; }

        public MessagePipeInProcessUdpOptions(string host, int port)
        {
            this.Host = host;
            this.Port = port;
            this.MessagePackSerializerOptions = ContractlessStandardResolver.Options;
            this.InstanceLifetime = InstanceLifetime.Scoped;
            this.UnhandledErrorHandler = (msg, x) => Console.WriteLine(msg + x);
        }
    }

    public sealed class MessagePipeInProcessNamedPipeOptions
    {
        public string PipeName { get; }
        public string ServerName { get; set; }
        public MessagePackSerializerOptions MessagePackSerializerOptions { get; set; }
        public InstanceLifetime InstanceLifetime { get; set; }
        public bool? AsServer { get; set; }
        public Action<string, Exception> UnhandledErrorHandler { get; set; }

        public MessagePipeInProcessNamedPipeOptions(string pipeName)
        {
            this.PipeName = pipeName;
            this.ServerName = ".";
            this.MessagePackSerializerOptions = ContractlessStandardResolver.Options;
            this.InstanceLifetime = InstanceLifetime.Scoped;
            this.UnhandledErrorHandler = (msg, x) => Console.WriteLine(msg + x);
            this.AsServer = null;
        }
    }

    public sealed class MessagePipeInProcessTcpOptions
    {
        public string Host { get; }
        public int Port { get; }
        public MessagePackSerializerOptions MessagePackSerializerOptions { get; set; }
        public InstanceLifetime InstanceLifetime { get; set; }
        public Action<string, Exception> UnhandledErrorHandler { get; set; }

        public MessagePipeInProcessTcpOptions(string host, int port)
        {
            this.Host = host;
            this.Port = port;
            this.MessagePackSerializerOptions = ContractlessStandardResolver.Options;
            this.InstanceLifetime = InstanceLifetime.Scoped;
            this.UnhandledErrorHandler = (msg, x) => Console.WriteLine(msg + x);
        }
    }
}
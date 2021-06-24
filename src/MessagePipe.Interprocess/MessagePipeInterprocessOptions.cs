using MessagePack;
using MessagePack.Resolvers;
using System;

namespace MessagePipe.Interprocess
{
    public abstract class MessagePipeInterprocessOptions
    {
        public MessagePackSerializerOptions MessagePackSerializerOptions { get; set; }
        public InstanceLifetime InstanceLifetime { get; set; }
        public Action<string, Exception> UnhandledErrorHandler { get; set; }

        public MessagePipeInterprocessOptions()
        {
            this.MessagePackSerializerOptions = ContractlessStandardResolver.Options;
            this.InstanceLifetime = InstanceLifetime.Scoped;
#if !UNITY_2018_3_OR_NEWER
            this.UnhandledErrorHandler = (msg, x) => Console.WriteLine(msg + x);
#else
            this.UnhandledErrorHandler = (msg, x) => UnityEngine.Debug.Log(msg + x);
#endif
        }
    }

    public sealed class MessagePipeInterprocessUdpOptions : MessagePipeInterprocessOptions
    {
        public string Host { get; }
        public int Port { get; }

        public MessagePipeInterprocessUdpOptions(string host, int port)
            : base()
        {
            this.Host = host;
            this.Port = port;
        }
    }

    public sealed class MessagePipeInterprocessNamedPipeOptions : MessagePipeInterprocessOptions
    {
        public string PipeName { get; }
        public string ServerName { get; set; }
        public bool? HostAsServer { get; set; }

        public MessagePipeInterprocessNamedPipeOptions(string pipeName)
            : base()
        {
            this.PipeName = pipeName;
            this.ServerName = ".";
            this.HostAsServer = null;
        }
    }

    public sealed class MessagePipeInterprocessTcpOptions : MessagePipeInterprocessOptions
    {
        public string Host { get; }
        public int Port { get; }
        public bool? HostAsServer { get; set; }

        public MessagePipeInterprocessTcpOptions(string host, int port)
            : base()
        {
            this.Host = host;
            this.Port = port;
            this.HostAsServer = null;
        }
    }
#if NET5_0_OR_GREATER
    public sealed class MessagePipeInterprocessUdpUdsOptions : MessagePipeInterprocessOptions
    {
        public string SocketPath { get; set; }

        public MessagePipeInterprocessUdpUdsOptions(string socketPath)
            : base()
        {
            this.SocketPath = socketPath;
        }

    }
    public sealed class MessagePipeInterprocessTcpUdsOptions : MessagePipeInterprocessOptions
    {
        public string SocketPath { get; set; }
        public int? SendBufferSize { get; set; }
        public int? ReceiveBufferSize { get; set; }
        public bool? HostAsServer { get; set; }
        public MessagePipeInterprocessTcpUdsOptions(string socketPath): this(socketPath, null, null)
        {
        }
        public MessagePipeInterprocessTcpUdsOptions(string socketPath, int? sendBufferSize, int? recvBufferSize)
        {
            this.SocketPath = socketPath;
            HostAsServer = null;
            this.SendBufferSize = sendBufferSize;
            this.ReceiveBufferSize = recvBufferSize;
        }
    }
#endif
}
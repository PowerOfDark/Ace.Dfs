using System.Collections.Generic;
using System.Runtime.Serialization;
using Ace.Networking.Threading;

namespace Ace.Dfs.Server.Structures
{
    [DataContract]
    public class DfsServerConfiguration
    {
        [DataMember]
        public int Port { get; set; }

        [DataMember]
        public string WebServerUrl { get; set; }

        [DataMember]
        public string ExternalWebServerUrl { get; set; }

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public string CertificatePath { get; set; }

        [DataMember]
        public string CertificatePassword { get; set; }

        [DataMember]
        public int? ShareSaveInterval { get; set; }

        [DataMember]
        public int PingInterval { get; set; }

        [DataMember]
        public int FileTransferTimeout { get; set; }

        [DataMember]
        public ThreadedQueueProcessorParameters SendQueue { get; set; }

        [DataMember]
        public ThreadedQueueProcessorParameters VerifyQueue { get; set; }

        [DataMember]
        public ThreadedQueueProcessorParameters UnzipQueue { get; set; }

        [DataMember]
        public Dictionary<string, DfsPermissions> RolePermissions { get; set; }
    }
}
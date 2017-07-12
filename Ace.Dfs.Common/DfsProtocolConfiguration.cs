using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking;
using Ace.Networking.MicroProtocol;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.ProtoBuf;

namespace Ace.Dfs.Common
{
    public class DfsProtocolConfiguration : ProtocolConfiguration
    {
        /// <summary>
        ///     Helper-field
        /// </summary>
        public const string VersionString = "v1.0.0";

        private static readonly object SingletonLock = new object();
        private static ProtocolConfiguration _instance;


        public DfsProtocolConfiguration()
        {
            var serializer = new GuidProtoBufSerializer();
            PayloadEncoder = new MicroEncoder(serializer.Clone());
            PayloadDecoder = new MicroDecoder(serializer.Clone());
            CustomOutcomingMessageQueue = GlobalOutcomingMessageQueue.Instance;
            CustomIncomingMessageQueue = GlobalIncomingMessageQueue.Instance;
            SslMode = SslMode.AuthorizationOnly;
            RequireClientCertificate = true;
            Initialize();
        }

        public static ProtocolConfiguration Instance
        {
            get
            {
                lock (SingletonLock)
                {
                    return _instance ?? (_instance = new DfsProtocolConfiguration());
                }
            }
        }

        protected override void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
            base.Initialize();
            GuidProtoBufSerializer.RegisterAssembly(typeof(DfsProtocolConfiguration).GetTypeInfo().Assembly);
        }

        public override ClientSslStreamFactory GetClientSslFactory(string targetCommonCame = "",
            X509Certificate certificate = null)
        {
            if ((SslMode != SslMode.None) & RequireClientCertificate && certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }
            return new ClientSslStreamFactory(targetCommonCame, certificate);
        }

        public override ServerSslStreamFactory GetServerSslFactory(X509Certificate certificate = null)
        {
            if (SslMode != SslMode.None && certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }
            return new ServerSslStreamFactory(certificate, RequireClientCertificate);
        }
    }
}
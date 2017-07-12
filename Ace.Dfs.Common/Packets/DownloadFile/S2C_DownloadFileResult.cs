using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.DownloadFile
{
    [ProtoContract]
    [Guid("DA77268D-FE33-4C50-8D45-B4AF36E8F0C9")]
    public class S2C_DownloadFileResult
    {
        public static readonly S2C_DownloadFileResult Failed = new S2C_DownloadFileResult(false);

        public S2C_DownloadFileResult(bool success = false)
        {
            Success = success;
        }

        protected S2C_DownloadFileResult()
        {
        }

        [ProtoMember(1)]
        public bool Success { get; protected set; }
    }
}
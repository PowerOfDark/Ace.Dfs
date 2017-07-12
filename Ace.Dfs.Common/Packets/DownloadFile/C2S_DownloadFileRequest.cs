using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.DownloadFile
{
    [ProtoContract]
    [Guid("7EA70A3C-E48B-4101-9E31-D0D7FE5EA659")]
    public class C2S_DownloadFileRequest
    {
        public C2S_DownloadFileRequest(int bufferId, string s64Sha256)
        {
            BufferId = bufferId;
            S64Sha256 = s64Sha256;
        }

        protected C2S_DownloadFileRequest()
        {
        }

        [ProtoMember(1)]
        public int BufferId { get; protected set; }

        [ProtoMember(2)]
        public string S64Sha256 { get; protected set; }
    }
}
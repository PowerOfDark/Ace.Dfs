using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.DownloadFile
{
    [ProtoContract]
    [Guid("0A5C4288-1779-4798-8724-C97EC07EC368")]
    public class C2S_DownloadFileReady
    {
        public C2S_DownloadFileReady(int bufferId)
        {
            BufferId = bufferId;
        }

        protected C2S_DownloadFileReady()
        {
        }

        [ProtoMember(1)]
        public int BufferId { get; protected set; }
    }
}
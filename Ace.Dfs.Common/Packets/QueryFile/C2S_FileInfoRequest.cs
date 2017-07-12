using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.QueryFile
{
    [ProtoContract]
    [Guid("899A925E-86AE-4814-9889-739468FFDCBD")]
    public class C2S_FileInfoRequest
    {
        public C2S_FileInfoRequest(string fileId)
        {
            FileId = fileId;
        }

        protected C2S_FileInfoRequest()
        {
        }

        [ProtoMember(1)]
        public string FileId { get; protected set; }
    }
}
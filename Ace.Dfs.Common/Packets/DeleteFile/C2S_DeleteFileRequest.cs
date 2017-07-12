using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.DeleteFile
{
    [ProtoContract]
    [Guid("52EFF5D8-C93A-4784-88EA-EA5302DB4248")]
    public class C2S_DeleteFileRequest
    {
        public C2S_DeleteFileRequest(string s64Sha256)
        {
            S64Sha256 = s64Sha256;
        }

        protected C2S_DeleteFileRequest()
        {
        }

        [ProtoMember(1)]
        public string S64Sha256 { get; protected set; }
    }
}
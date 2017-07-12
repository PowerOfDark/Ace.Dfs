using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.UnzipFile
{
    [ProtoContract]
    [Guid("160C48AA-128C-4BE5-BCBB-E9014561BB36")]
    public class C2S_UnzipFileRequest
    {
        public C2S_UnzipFileRequest(string s64Sha256, string regex = null)
        {
            S64Sha256 = s64Sha256;
            RegexFilter = regex;
        }

        protected C2S_UnzipFileRequest()
        {
        }

        [ProtoMember(1)]
        public string S64Sha256 { get; protected set; }

        [ProtoMember(2)]
        public string RegexFilter { get; protected set; }
    }
}
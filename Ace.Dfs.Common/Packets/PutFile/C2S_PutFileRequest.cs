using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.PutFile
{
    [ProtoContract]
    [Guid("8A3611BA-5C46-407A-8EAF-A7607AFA1A93")]
    public class C2S_PutFileRequest
    {
        public C2S_PutFileRequest(string s64Sha256, long contentLength)
        {
            S64Sha256 = s64Sha256;
            ContentLength = contentLength;
        }

        protected C2S_PutFileRequest()
        {
        }

        [ProtoMember(1)]
        public string S64Sha256 { get; protected set; }

        [ProtoMember(2)]
        public long ContentLength { get; protected set; }
    }
}
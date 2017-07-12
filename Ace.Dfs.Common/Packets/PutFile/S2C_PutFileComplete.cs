using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.PutFile
{
    [ProtoContract]
    [Guid("53D78338-2997-4E05-80F5-BA46C2F3AE58")]
    public class S2C_PutFileComplete
    {
        public S2C_PutFileComplete(string s64sha256, PutFileStatus status)
        {
            S64Sha256 = s64sha256;
            Status = status;
        }

        protected S2C_PutFileComplete()
        {
        }

        [ProtoMember(1)]
        public string S64Sha256 { get; protected set; }

        [ProtoMember(2)]
        public PutFileStatus Status { get; protected set; }
    }
}
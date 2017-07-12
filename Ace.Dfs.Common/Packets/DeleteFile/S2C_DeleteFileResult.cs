using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.DeleteFile
{
    [ProtoContract]
    [Guid("D9D4665A-38EB-4978-893D-48954BCEAE15")]
    public class S2C_DeleteFileResult
    {
        public S2C_DeleteFileResult(bool success)
        {
            Success = success;
        }

        protected S2C_DeleteFileResult()
        {
        }

        [ProtoMember(1)]
        public bool Success { get; protected set; }
    }
}
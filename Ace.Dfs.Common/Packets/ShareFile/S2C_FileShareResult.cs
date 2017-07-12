using System.Runtime.InteropServices;
using Ace.Dfs.Common.Structures;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.ShareFile
{
    [ProtoContract]
    [Guid("B489CEFF-B764-4E92-A1DE-C4DBE5A12D1E")]
    public class S2C_FileShareResult
    {
        public S2C_FileShareResult(FileShareResult result)
        {
            ShareUrl = result?.ShareUrl;
            Success = result?.Success ?? false;
        }

        protected S2C_FileShareResult()
        {
        }

        [ProtoMember(1)]
        public string ShareUrl { get; protected set; }
        [ProtoMember(2)]
        public bool Success { get; protected set; }
    }
}
using System.Runtime.InteropServices;
using Ace.Dfs.Common.Structures;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.ShareFile
{
    [ProtoContract]
    [Guid("BD1D730F-4E78-4662-845B-0491D151656A")]
    public class C2S_FileShareRequest
    {
        public C2S_FileShareRequest(FileShare share)
        {
            Share = share;
        }

        protected C2S_FileShareRequest()
        {
        }

        [ProtoMember(1)]
        public FileShare Share { get; protected set; }
    }
}
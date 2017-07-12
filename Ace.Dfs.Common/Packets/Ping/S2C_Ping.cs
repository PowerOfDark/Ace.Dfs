using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.Ping
{
    [ProtoContract]
    [Guid("06E553B6-9619-4FB5-9968-3E34EAA19A81")]
    public class S2C_Ping
    {
        public static readonly S2C_Ping Instance = new S2C_Ping();
    }
}
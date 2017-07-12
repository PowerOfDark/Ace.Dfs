using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.Ping
{
    [ProtoContract]
    [Guid("C3EFD41D-161D-46D1-A269-02116AC413BF")]
    public class C2S_Ping
    {
        public static readonly C2S_Ping Instance = new C2S_Ping();
    }
}
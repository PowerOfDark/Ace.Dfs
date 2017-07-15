using System.Runtime.InteropServices;
using Ace.Dfs.Common.Structures;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets
{
    [ProtoContract]
    [Guid("A432183E-E381-45BD-BFED-F8933521B624")]
    public class S2C_FileInfoResult
    {
        public S2C_FileInfoResult(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
        }

        protected S2C_FileInfoResult()
        {
        }

        [ProtoMember(1)]
        public FileInfo FileInfo { get; protected set; }

        //[NotMapped]
        public bool FileExists => FileInfo?.FileExists ?? false;
    }
}
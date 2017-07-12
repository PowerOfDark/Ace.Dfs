using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using Ace.Dfs.Common.Structures;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.DownloadFile
{
    [ProtoContract]
    [Guid("6635F321-6539-4179-B96F-0EB96E1C9668")]
    public class S2C_DownloadFileReady
    {
        public S2C_DownloadFileReady(FileInfo fileInfo = null)
        {
            FileInfo = fileInfo;
        }

        protected S2C_DownloadFileReady()
        {
        }

        [ProtoMember(1)]
        public FileInfo FileInfo { get; protected set; }

        [NotMapped]
        public bool Success => FileInfo?.FileExists ?? false;
    }
}
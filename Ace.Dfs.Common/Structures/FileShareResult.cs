using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Structures
{
    [ProtoContract]
    [Guid("024F5706-86F8-4ADB-B8A8-10D58E28C089")]
    public class FileShareResult
    {
        public FileShareResult(FileShare share, string url)
        {
            Share = share;
            ShareUrl = url;
            Success = true;
        }

        public FileShareResult(FileShare share)
        {
            Share = share;
            Success = false;
        }

        protected FileShareResult()
        {
        }

        [ProtoMember(1)]
        public FileShare Share { get; protected set; }

        [ProtoMember(2)]
        public string ShareUrl { get; protected set; }

        [ProtoMember(3)]
        public bool Success { get; protected set; }
    }
}
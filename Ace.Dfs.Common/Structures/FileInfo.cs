using System;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Structures
{
    [ProtoContract]
    [Guid("F4A2CE48-E55F-4C53-A3F9-E70DF3FB6B5F")]
    public class FileInfo
    {
        public FileInfo(string s64Sha256, long length, DateTime cDate, DateTime aDate)
        {
            FileExists = true;
            ContentLength = length;
            CreationDate = cDate;
            LastAccessedDate = aDate;
            S64Sha256 = s64Sha256;
        }

        public FileInfo()
        {
            FileExists = false;
        }

        [ProtoMember(1)]
        public bool FileExists { get; protected set; }

        [ProtoMember(2)]
        public long ContentLength { get; protected set; }

        [ProtoMember(3)]
        public DateTime CreationDate { get; protected set; }

        [ProtoMember(4)]
        public DateTime LastAccessedDate { get; protected set; }

        [ProtoMember(5)]
        public string S64Sha256 { get; protected set; }
    }
}
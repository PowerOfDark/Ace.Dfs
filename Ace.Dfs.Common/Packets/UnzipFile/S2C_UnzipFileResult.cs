using System.Collections.Generic;
using System.Runtime.InteropServices;
using Ace.Dfs.Common.Structures;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.UnzipFile
{
    [ProtoContract]
    [Guid("D63ECD8C-4918-4C44-85F3-63965A18A021")]
    public class S2C_UnzipFileResult
    {
        public S2C_UnzipFileResult(List<KeyValuePair<string, FileInfo>> extracted, bool finished, bool valid)
        {
            ExtractedFiles = extracted;
            Finished = finished;
            ValidArchive = valid;
        }

        protected S2C_UnzipFileResult()
        {
        }

        [ProtoMember(1)]
        public List<KeyValuePair<string, FileInfo>> ExtractedFiles { get; protected set; }

        [ProtoMember(2)]
        public bool Finished { get; protected set; }

        [ProtoMember(3)]
        public bool ValidArchive { get; protected set; }
    }
}
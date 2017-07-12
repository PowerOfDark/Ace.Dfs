using System;
using System.IO;
using Ace.Dfs.Common.Packets.PutFile;
using Ace.Networking;

namespace Ace.Dfs.Server.Structures
{
    public class IncomingFileStatus
    {
        public int DownloadedParts;
        public C2S_PutFileRequest Request { get; set; }
        public FileStream FileStream { get; set; }
        public Connection Connection { get; set; }
        public DateTime LastPart { get; set; }
    }
}
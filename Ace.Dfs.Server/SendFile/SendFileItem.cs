using System.IO;
using Ace.Networking.Handlers;

namespace Ace.Dfs.Server.SendFile
{
    public class SendFileItem
    {
        public RequestWrapper Request { get; set; }
        public FileStream FileStream { get; set; }
        public int BufferId { get; set; }
    }
}
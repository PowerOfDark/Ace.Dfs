using System.IO;
using Ace.Networking.Handlers;

namespace Ace.Dfs.Server.UnzipFile
{
    public class UnzipFileItem
    {
        public RequestWrapper Request { get; set; }
        public FileStream FileStream { get; set; }
    }
}
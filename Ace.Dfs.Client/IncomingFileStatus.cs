using System.IO;
using System.Threading.Tasks;
using FileInfo = Ace.Dfs.Common.Structures.FileInfo;

namespace Ace.Dfs.Client
{
    public class IncomingFileStatus
    {
        public int DownloadedParts;
        public TaskCompletionSource<string> TaskCompletionSource { get; set; }
        public FileStream FileStream { get; set; }
        public FileInfo FileInfo { get; set; }
        public bool VerifyHash { get; set; }
    }
}
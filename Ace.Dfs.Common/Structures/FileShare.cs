using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using ProtoBuf;

namespace Ace.Dfs.Common.Structures
{
    [DataContract]
    [ProtoContract]
    [Guid("4A255D91-B1FE-460B-90B6-AB33F169DCB5")]
    public class FileShare
    {
        [DataMember(Order = 3)] [ProtoMember(3)] public int DownloadLimit;

        public FileShare(string fileId, string downloadName, int downloadLimit = int.MinValue, bool forceDownload = false)
        {
            FileId = fileId;
            DownloadName = downloadName;
            ExpirationDate = DateTime.MinValue;
            DownloadLimit = downloadLimit;
            ForceDownload = forceDownload;
        }

        public FileShare(string fileId, string downloadName, DateTime expirationDate, int downloadLimit = int.MinValue,
            bool forceDownload = false) : this(fileId, downloadName, downloadLimit, forceDownload)
        {
            ExpirationDate = expirationDate;
        }

        protected FileShare()
        {
        }

        [DataMember(Order = 1)]
        [ProtoMember(1)]
        public string FileId { get; set; }

        [DataMember(Order = 2)]
        [ProtoMember(2)]
        public DateTime ExpirationDate { get; set; }

        [DataMember(Order = 4)]
        [ProtoMember(4)]
        public string DownloadName { get; set; }

        [DataMember(Order = 5)]
        [ProtoMember(5)]
        public bool ForceDownload { get; set; }

        public bool IsExpired(DateTime now)
        {
            return DownloadLimit != int.MinValue && DownloadLimit <= 0 || ExpirationDate != DateTime.MinValue && ExpirationDate < now;
        }

        /// <summary>
        ///     Action called on file download
        /// </summary>
        /// <returns>False if expired after the action</returns>
        public bool Download()
        {
            if (DownloadLimit != int.MinValue)
            {
                return Interlocked.Decrement(ref DownloadLimit) > 0;
            }
            return true;
        }
    }
}
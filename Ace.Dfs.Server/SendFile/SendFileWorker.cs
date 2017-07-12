using System;
using System.IO;
using Ace.Dfs.Common;
using Ace.Dfs.Common.Packets.DownloadFile;
using Ace.Networking.Interfaces;

namespace Ace.Dfs.Server.SendFile
{
    internal class SendFileWorker : IWorker<SendFileItem>
    {
        public void DoWork(SendFileItem item)
        {
            if (!item.Request.Connection.Connected)
            {
                return;
            }
            var allParts = DfsConfiguration.GetNumberOfChunks(item.FileStream.Length);
            var fs = item.FileStream;
            var left = fs.Length;
            try
            {
                using (fs)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    for (var i = 0; i < allParts; i++)
                    {
                        var toSend = Math.Min(DfsConfiguration.ChunkSize, left);
                        item.Request.Connection.EnqueueSendRaw(item.BufferId, i, fs, (int) toSend, false).GetAwaiter().GetResult();
                        left -= toSend;
                    }
                }
            }
            catch
            {
            }

            item.Request.TrySendResponse(new S2C_DownloadFileResult(left == 0), out _);
        }
    }
}
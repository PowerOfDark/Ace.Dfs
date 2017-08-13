using System;
using System.IO;
using Ace.Dfs.Common;
using Ace.Dfs.Common.Packets.PutFile;
using Ace.Dfs.Server.Helpers;
using Ace.Dfs.Server.Structures;
using Ace.Networking;
using Ace.Networking.Interfaces;

namespace Ace.Dfs.Server.VerifyFile
{
    public class VerifyFileWorker : IWorker<IncomingFileStatus>
    {
        public void DoWork(IncomingFileStatus item)
        {
            var fs = item.FileStream;
            var localName = fs.Name;
            bool error = false;

            string s64Sha256 = null;

            using (fs)
            {
                if (FileManager.FileExists(item.Request.S64Sha256))
                {
                    if (item.Connection.Connected)
                        item.Connection.Send(new S2C_PutFileComplete(item.Request.S64Sha256, PutFileStatus.FileExists));
                    try
                    {
                        File.Delete(localName);
                    }
                    catch
                    {
                    }
                    return;
                }

                fs.Flush();
                error = fs.Length != item.Request.ContentLength;

                if (!error)
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    s64Sha256 = fs.S64Sha256L();
                    if (s64Sha256 != item.Request.S64Sha256)
                    {
                        error = true;
                    }
                }
            }
            if (!error)
            {
                try
                {
                    FileManager.PutLocal(localName, s64Sha256, true);
                }
                catch (Exception e)
                {
                    error = !FileManager.FileExists(s64Sha256);
                }
            }

            try
            {
                File.Delete(localName);
            }
            catch
            {
            }

            if (item.Connection.Connected)
            {
                item.Connection.Send(new S2C_PutFileComplete(item.Request.S64Sha256,
                    error ? PutFileStatus.Error : PutFileStatus.Complete));
            }
        }
    }
}
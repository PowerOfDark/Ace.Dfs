using System.Collections.Generic;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Ace.Dfs.Common;
using Ace.Dfs.Common.Packets.UnzipFile;
using Ace.Dfs.Common.Structures;
using Ace.Dfs.Server.Helpers;
using Ace.Networking.Interfaces;

namespace Ace.Dfs.Server.UnzipFile
{
    internal class UnzipFileWorker : IWorker<UnzipFileItem>
    {
        public void DoWork(UnzipFileItem item)
        {
            var valid = false;
            List<KeyValuePair<string, FileInfo>> extracted = null;
            var finished = false;

            var req = item.Request.Request as C2S_UnzipFileRequest;
            var fs = item.FileStream;
            using (fs)
            {
                try
                {
                    var archive = new ZipArchive(fs, ZipArchiveMode.Read, true);
                    var entries = archive.Entries; //load and verify 
                    valid = true;
                    var filter = req?.RegexFilter != null;
                    Regex regex = null;
                    if (filter)
                    {
                        regex = new Regex(req.RegexFilter);
                    }
                    extracted = new List<KeyValuePair<string, FileInfo>>();
                    foreach (var entry in entries)
                    {
                        if (!filter || regex.IsMatch(entry.FullName))
                        {
                            try
                            {
                                var path = PathHelper.CreateTemporaryFilePath(Config.Dfs.Path);
                                entry.ExtractToFile(path, true);
                                var hash = FileManager.PutLocal(path, null, true);
                                if (hash != null)
                                {
                                    var qfi = FileManager.GetFileInfo(hash);
                                    if (qfi.FileExists)
                                    {
                                        extracted.Add(new KeyValuePair<string, FileInfo>(entry.FullName, qfi));
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                    finished = true;
                }
                catch
                {
                }
            }

            item.Request.TrySendResponse(new S2C_UnzipFileResult(extracted, finished, valid), out _);
        }
    }
}
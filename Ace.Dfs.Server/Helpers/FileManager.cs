using System;
using System.IO;
using Ace.Dfs.Common;
using Ace.Dfs.Common.Structures;
using FileInfo = Ace.Dfs.Common.Structures.FileInfo;
using FileShare = Ace.Dfs.Common.Structures.FileShare;

namespace Ace.Dfs.Server.Helpers
{
    public static class FileManager
    {
        public static FileInfo GetFileInfo(string id, string path = null)
        {
            if (!CryptoHelper.IsHashValid(id))
            {
                return null;
            }
            if (path == null)
            {
                path = PathHelper.MapLocal(id, Config.Dfs.Path);
            }
            var f = new System.IO.FileInfo(path);
            if (!f.Exists)
            {
                return null;
            }
            var ret = new FileInfo(id, f.Length, f.CreationTime, f.LastAccessTime);
            UpdateLastAccessed(f.FullName);
            return ret;
        }

        public static bool FileExists(string id)
        {
            return FileExists(id, out _);
        }

        public static bool FileExists(string id, out string path)
        {
            var f = PathHelper.FileExists(id, out path, Config.Dfs.Path);
            if (f)
            {
                //update access time
                UpdateLastAccessed(path);
            }
            return f;
        }

        public static void UpdateLastAccessed(string path)
        {
            Console.WriteLine($"bumped last accessed on {path}");
            File.SetLastAccessTime(path, DateTime.Now);
        }

        public static string PutLocal(string filename, string s64Sha256 = null, bool move = false)
        {
            var key = s64Sha256;
            if (key == null)
            {
                using (var fs = File.Open(filename, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read))
                {
                    key = fs.S64Sha256();
                }
            }

            if (FileExists(key))
            {
                if (move)
                {
                    try
                    {
                        File.Delete(filename);
                    }
                    catch
                    {
                    }
                }
                return key;
            }

            var f = new System.IO.FileInfo(PathHelper.MapLocal(key, Config.Dfs.Path));
            f.Directory.Create();
            if (move)
            {
                File.Move(filename, f.FullName);
            }
            else
            {
                File.Copy(filename, f.FullName, false);
            }
            return key;
        }

        public static FileShareResult Share(FileShare share)
        {
            if (share == null)
            {
                return new FileShareResult(null);
            }
            //check for cheeky directory traversal attempts
            if (!CryptoHelper.IsHashValid(share?.FileId))
            {
                //TODO: make the user regret this
                return new FileShareResult(share);
            }
            if (!FileExists(share.FileId))
            {
                return new FileShareResult(share);
            }
            var urlKey = WebServer.AddShare(share);
            var res = new FileShareResult(share, Config.Dfs.ExternalWebServerUrl + "/" + urlKey);
            return res;
        }

        public static void CleanEmptyDirectories(System.IO.FileInfo target)
        {
            var dir = target.Directory;
            try
            {
                for (var i = 0; i < PathHelper.Depth && dir.Exists; i++)
                {
                    dir.Delete();
                    dir = dir.Parent;
                }
            }
            catch
            {
            }
        }

        public static bool Delete(string id)
        {
            if (!CryptoHelper.IsHashValid(id))
            {
                return false;
            }
            if (!FileExists(id, out var path))
            {
                return true;
            }
            var ret = false;
            try
            {
                var fi = new System.IO.FileInfo(path);
                fi.Delete();
                ret = true;
                CleanEmptyDirectories(fi);
            }
            catch
            {
            }
            return ret;
        }
    }
}
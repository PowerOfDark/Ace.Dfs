using System;
using System.IO;

namespace Ace.Dfs.Common
{
    public static class PathHelper
    {
        public const int MaxHashLength = 44;
        public const int Depth = 6; // 44 chars - 12 = 32 chars for file name

        public static string MapLocal(string hash, string path = "")
        {
            if (hash.Length <= Depth * 2)
            {
                return null;
            }
            var ret = path;
            for (var i = 0; i < Depth; i++)
            {
                ret = Path.Combine(ret, hash.Substring(i * 2, 2));
            }
            ret = Path.Combine(ret, hash.Substring(Depth * 2));
            return ret;
        }

        public static bool FileExists(string hash, string path = "")
        {
            return File.Exists(MapLocal(hash, path));
        }

        public static bool FileExists(string hash, out string resolvedPath, string path = "")
        {
            resolvedPath = MapLocal(hash, path);
            return File.Exists(resolvedPath);
        }

        public static string CreateTemporaryFilePath(string path = "")
        {
            path = Path.Combine(path, "temp");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string tmp;
            do
            {
                tmp = CryptoHelper.ToSafe64String(Guid.NewGuid().ToByteArray());
            } while (File.Exists(Path.Combine(path, tmp)));
            return Path.Combine(path, tmp);
        }

        public static FileStream CreateTemporaryFile(string path = "")
        {
            return File.Create(CreateTemporaryFilePath(path));
        }

        public static void ClearTempDirectory(string path = "")
        {
            try
            {
                path = Path.Combine(path, "temp");
                if(Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch { }
        }

        public static string MapHashFromLocal(string path)
        {
            var depth = -1;
            var i = path.Length - 1;
            var ch = new char[MaxHashLength];
            var j = ch.Length - 1;
            while (depth < Depth && i >= 0 && j >= 0)
            {
                var c = path[i--];
                if (c == '/' || c == '\\')
                {
                    depth++;
                }
                else
                {
                    ch[j--] = c;
                }
            }
            if (Depth != depth)
            {
                return null;
            }
            return new string(ch, j + 1, ch.Length - j - 1);
        }
    }
}
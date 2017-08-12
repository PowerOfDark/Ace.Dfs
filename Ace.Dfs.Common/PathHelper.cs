using System;
using System.IO;

namespace Ace.Dfs.Common
{
    public static class PathHelper
    {
        public const int MaxHashLength = 44;
        public const int MinHashLength = MaxHashLength - 4;
        public const int MaxHandleLength = MaxHashLength + LLength;
        public const int LLength = 12;
        public const int Depth = 7; // 56 chars - 14 = 42 chars for file name

        public static string MapLocal(string handle, string path = "", int depth = Depth)
        {
            if (handle.Length <= depth * 2)
            {
                return null;
            }
            var ret = path;
            for (var i = 0; i < depth; i++)
            {
                ret = Path.Combine(ret, handle.Substring(i * 2, 2));
            }
            ret = Path.Combine(ret, handle.Substring(depth * 2));
            return ret;
        }

        public static bool FileExists(string handle, string path = "")
        {
            return File.Exists(MapLocal(handle, path));
        }

        public static bool FileExists(string handle, out string resolvedPath, string path = "")
        {
            resolvedPath = MapLocal(handle, path);
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

        public static bool TryDecodeHandle(string handle, out (string S64Sha256, long Length) result)
        {
            result = default((string, long));
            if (handle == null || (handle.Length) > MaxHandleLength || (handle.Length - LLength) < MinHashLength) return false;
            var len = handle.Substring(handle.Length - LLength, LLength);
            result.Length = BitConverter.ToInt64(CryptoHelper.DecodeSafe64String(len), 0);
            result.S64Sha256 = handle.Substring(0, handle.Length - LLength);
            return true;
        }

        public static string MapHandleFromLocal(string path)
        {
            var depth = -1;
            var i = path.Length - 1;
            var ch = new char[MaxHandleLength];
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
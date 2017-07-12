using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Ace.Dfs.Common
{
    public static class CryptoHelper
    {
        private static readonly Regex ValidateHash = new Regex("[^a-zA-Z0-9_-]", RegexOptions.Compiled);

        private static readonly char[] padding = {'='};

        public static string S64Sha256(string str)
        {
            using (var sha = SHA256.Create())
            {
                return ToSafe64String(sha.ComputeHash(Encoding.UTF8.GetBytes(str)));
            }
        }

        public static string S64Sha256(this FileStream fs)
        {
            using (var sha = SHA256.Create())
            {
                return ToSafe64String(sha.ComputeHash(fs));
            }
        }

        public static string ToSafe64String(byte[] buf)
        {
            return Convert.ToBase64String(buf)
                .TrimEnd(padding).Replace('+', '-').Replace('/', '_');
        }


        public static byte[] DecodeSafe64String(string str)
        {
            var incoming = str
                .Replace('_', '/').Replace('-', '+');
            switch (incoming.Length % 4)
            {
                case 2:
                    incoming += "==";
                    break;
                case 3:
                    incoming += "=";
                    break;
            }
            return Convert.FromBase64String(incoming);
        }

        public static bool IsHashValid(string str)
        {
            return !ValidateHash.IsMatch(str);
        }

        public static int BuildDiscriminatorFromS64(string s64)
        {
            var bytes = DecodeSafe64String(s64.Substring(0, 8));
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
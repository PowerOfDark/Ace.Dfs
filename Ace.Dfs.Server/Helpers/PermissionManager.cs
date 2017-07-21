using System;
using System.Runtime.CompilerServices;
using Ace.Dfs.Server.Structures;
using Ace.Networking;
using Ace.Networking.MicroProtocol.SSL;

namespace Ace.Dfs.Server.Helpers
{
    public static class PermissionManager
    {
        public static bool Authorize(Connection connection)
        {
            var roleStr = connection.SslCertificates.RemoteCertificate.Subject;
            var role = BasicCertificateInfo.GetAttribute(roleStr, CertificateAttribute.GivenName, "");
            if (!Config.Dfs.RolePermissions.ContainsKey(role))
            {
                return false;
            }
            var perm = Config.Dfs.RolePermissions[role];
            connection.Data["_R"] = role;
            connection.Data["_"] = (int) perm;
            return true;
        }

        public static bool Authorized(this Connection connection, DfsPermissions requiredPerms, [CallerMemberName] string method = "")
        {
            var num = (int) requiredPerms;
            var has = connection.Data.Get("_", 0);
            var missing = ~has & num;
            if (missing != 0)
            {
                var name = BasicCertificateInfo.GetAttribute(connection.SslCertificates.RemoteCertificate.Subject,
                    CertificateAttribute.CommonName, null);
                Console.WriteLine(
                    $"[Permissions] User {name} from {connection.Socket.RemoteEndPoint} tried to {method}, missing permissions: {(DfsPermissions) missing}");
                return false;
            }
            Console.WriteLine($"[Permissions] {connection.Socket.RemoteEndPoint}: {method}");
            return true;
        }
    }
}
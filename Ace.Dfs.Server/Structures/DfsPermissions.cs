using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ace.Dfs.Server.Structures
{
    [DataContract]
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DfsPermissions
    {
        None = 0,
        QueryFile = 1,
        DownloadFile = 2,
        PutFile = 4,
        DeleteFile = 8,
        ShareFile = 16,
        UnzipFile = 32,
        All = int.MaxValue,
        Shard = QueryFile | DownloadFile | PutFile,
        Realm = All
    }
}
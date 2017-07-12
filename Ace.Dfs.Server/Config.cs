using System;
using System.Collections.Generic;
using System.IO;
using Ace.Dfs.Server.Structures;
using Ace.Networking.Threading;
using Newtonsoft.Json;

namespace Ace.Dfs.Server
{
    public static class Config
    {
        public static DfsServerConfiguration Dfs;

        public static void Save()
        {
            using (var fs = File.Open("dfs.json", FileMode.Create, FileAccess.Write))
            {
                using (var sw = new StreamWriter(fs))
                {
                    JsonSerializer.CreateDefault().Serialize(sw, Dfs);
                }
            }
        }

        public static void Default()
        {
            var verify = new ThreadedQueueProcessorParameters
            {
                PreservePartitioning = true,
                MinThreads = 4,
                MaxThreads = 8,
                MaxThreadsPerClient = null,
                BoostBarrier = 4
            };

            var send = new ThreadedQueueProcessorParameters
            {
                MinThreads = 4,
                MaxThreads = 100,
                MaxThreadsPerClient = 2,
                BoostBarrier = 2,
                PreservePartitioning = true
            };

            var unzip = new ThreadedQueueProcessorParameters
            {
                MinThreads = 4,
                MaxThreads = 8,
                MaxThreadsPerClient = 1,
                BoostBarrier = 4,
                PreservePartitioning = true
            };

            var perms = new Dictionary<string, DfsPermissions>
            {
                {"Realm", DfsPermissions.All},
                {"Shard", DfsPermissions.Shard}
            };

            Dfs = new DfsServerConfiguration
            {
                WebServerUrl = "http://localhost:52357",
                Port = 0xAceF,
                Path = "E:\\Ace-Dfs-Store\\",
                ShareSaveInterval = 30_000,
                PingInterval = 10_000,
                FileTransferTimeout = 60_000,
                SendQueue = send,
                VerifyQueue = verify,
                UnzipQueue = unzip,
                RolePermissions = perms
            };
            Dfs.ExternalWebServerUrl = Dfs.WebServerUrl;
        }

        public static void Load()
        {
            if (!File.Exists("dfs.json"))
            {
                Default();
                Save();
                return;
            }
            using (var fs = File.OpenRead("dfs.json"))
            {
                using (var sr = new StreamReader(fs))
                {
                    Dfs = (DfsServerConfiguration) JsonSerializer.CreateDefault().Deserialize(sr, typeof(DfsServerConfiguration));
                }
            }
            if (Dfs.Path == null)
            {
                throw new InvalidDataException("Invalid config");
            }
            if (!Directory.Exists(Dfs.Path))
            {
                Directory.CreateDirectory(Dfs.Path);
            }
            Dfs.PingInterval = Math.Max(Dfs.PingInterval, 1000);
            if (Dfs.ShareSaveInterval.HasValue)
            {
                Dfs.ShareSaveInterval = Math.Max(Dfs.ShareSaveInterval.Value, 5000);
            }
            Dfs.FileTransferTimeout = Math.Max(Dfs.FileTransferTimeout, 1000);
        }
    }
}
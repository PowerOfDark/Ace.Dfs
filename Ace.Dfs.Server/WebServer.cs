using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Ace.Dfs.Common;
using Ace.Dfs.Server.Structures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FileShare = Ace.Dfs.Common.Structures.FileShare;

namespace Ace.Dfs.Server
{
    public static class WebServer
    {
        public static ConcurrentDictionary<string, FileShare> Shares { get; private set; } = new ConcurrentDictionary<string, FileShare>();


        public static IWebHost WebHost { get; private set; }

        private static void Configure(DfsServerConfiguration config)
        {
            if (WebHost != null)
            {
                return;
            }
            var cfg = new ConfigurationBuilder().Build();
            var builder = new WebHostBuilder().UseStartup<Startup>().UseKestrel().UseUrls(config.WebServerUrl);

            WebHost = builder.Build();
        }

        public static void Start(DfsServerConfiguration config)
        {
            if (WebHost != null)
            {
                return;
            }
            Configure(config);
            WebHost?.Start();
        }

        public static void Run(DfsServerConfiguration config)
        {
            if (WebHost != null)
            {
                return;
            }
            Configure(config);
            WebHost?.Run();
        }

        public static string AddShare(FileShare share)
        {
            var key = "";
            do
            {
                key = CryptoHelper.ToSafe64String(Guid.NewGuid().ToByteArray());
            } while (Shares.ContainsKey(key));

            return Shares.TryAdd(key, share) ? key : null;
        }

        public static void SaveShares()
        {
            var now = DateTime.Now;
            foreach (var kv in Shares)
            {
                if (kv.Value.IsExpired(now))
                {
                    Shares.TryRemove(kv.Key, out _);
                }
            }
            using (var fs = File.Open("shares.json", FileMode.Create, FileAccess.Write, System.IO.FileShare.None))
            {
                using (var sw = new StreamWriter(fs))
                {
                    JsonSerializer.CreateDefault().Serialize(sw, Shares);
                }
            }
        }

        public static void LoadShares()
        {
            if (!File.Exists("shares.json"))
            {
                return;
            }
            using (var fs = File.Open("shares.json", FileMode.Open, FileAccess.Read))
            {
                using (var sr = new StreamReader(fs))
                {
                    Shares = (ConcurrentDictionary<string, FileShare>) JsonSerializer.CreateDefault()
                        .Deserialize(sr, typeof(ConcurrentDictionary<string, FileShare>));
                }
            }
            if (Shares == null)
            {
                Shares = new ConcurrentDictionary<string, FileShare>();
            }
            var now = DateTime.Now;
            foreach (var kv in Shares)
            {
                if (kv.Value.IsExpired(now))
                {
                    Shares.TryRemove(kv.Key, out _);
                }
            }
        }

        public static async Task HandleRequest(HttpContext context)
        {
            var now = DateTime.Now;
            if (context.Request.Path.Value == "/")
            {
                context.Response.ContentType = "text/html";
                await context.Response
                    .WriteAsync($"<h1>Ace Distributed File System {DfsProtocolConfiguration.VersionString}<h1>");
            }
            else
            {
                var key = context.Request.Path.Value.Substring(1);
                if (!Shares.TryGetValue(key, out var share))
                {
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    await context.Response.WriteAsync("<h1>Not found</h1>");
                    return;
                }
                var expired = false;
                lock (share)
                {
                    var toDelete = false;
                    if (share.IsExpired(now))
                    {
                        toDelete = expired = true;
                    }
                    else
                    {
                        // this will also proc on the last download (since number of downloads left will equal 0)
                        toDelete = !share.Download();
                    }
                    if (toDelete)
                    {
                        Shares.TryRemove(key, out _);
                    }
                }
                if (expired)
                {
                    context.Response.StatusCode = (int) HttpStatusCode.Gone;
                    await context.Response.WriteAsync("<h1>No longer available</h1>");
                    return;
                }

                string header;
                if (share.ForceDownload)
                {
                    header = "attachment; filename=" + share.DownloadName;
                }
                else
                {
                    header = "filename=" + share.DownloadName;
                }

                context.Response.Headers.Append("Content-Disposition", header);
                try
                {
                    await context.Response.SendFileAsync(PathHelper.MapLocal(share.FileId, Config.Dfs.Path)).ConfigureAwait(false);
                }
                catch { }
            }
        }


        public class Startup
        {
            public Startup(IHostingEnvironment env)
            {
                Configuration = new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();
            }

            public IConfigurationRoot Configuration { get; }

            #region snippet_Configure

            public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
            {
                loggerFactory.AddConsole();
                app.Run(HandleRequest);
            }

            #endregion
        }
    }
}
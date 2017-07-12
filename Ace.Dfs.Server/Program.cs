using System;
using System.Threading;
using Ace.Dfs.Common;

namespace Ace.Dfs.Server
{
    internal class Program
    {
        public static Timer Timer = new Timer(Monitor_Tick, null, Timeout.Infinite, Timeout.Infinite);

        private static void Monitor_Tick(object state)
        {
            if (WebServer.WebHost != null)
            {
                try
                {
                    WebServer.SaveShares();
                }
                catch
                {
                }
            }
            if (Config.Dfs.ShareSaveInterval.HasValue)
            {
                Timer.Change(Config.Dfs.ShareSaveInterval.Value, Timeout.Infinite);
            }
        }

        private static void Main(string[] args)
        {
            Config.Load();
            PathHelper.ClearTempDirectory(Config.Dfs.Path);

            WebServer.LoadShares();
            WebServer.Start(Config.Dfs);
            if (Config.Dfs.ShareSaveInterval.HasValue)
            {
                Timer.Change(Config.Dfs.ShareSaveInterval.Value, Timeout.Infinite);
            }

            DfsServer.Start();

            Console.WriteLine("Type QUIT to exit");
            string str;
            while ((str = Console.ReadLine()) != "QUIT")
            {
            }
            Monitor_Tick(null);
        }
    }
}
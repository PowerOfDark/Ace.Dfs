using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ace.Dfs.Client;
using Ace.Dfs.Common;
using Ace.Networking.MicroProtocol.Headers;
using System.IO;
using System.Linq;

namespace Ace.Dfs.TestClient
{
    internal class Program
    {
        public static DfsClient client;

        private static void Main(string[] args)
        {
            var cert = new X509Certificate2(@"D:\cert\openssl\ace.realm\device.pfx", "acerealm");
            var cl = new TcpClient();
            while (true)
            {
                try
                {
                    cl.ConnectAsync("localhost", 0xAceF).GetAwaiter().GetResult();
                    break;
                }
                catch
                {
                    Thread.Sleep(500);
                }
            }
            var cfg = DfsProtocolConfiguration.Instance;
            var ssl = cfg.GetClientSslFactory("ace.dfs", cert);

            client = new DfsClient(ssl, cl, "D:\\Ace-Dfs-Client-Cache");
            client.Initialize();

            Task.Factory.StartNew(Test);
            Console.ReadKey();
        }

        private static async void Test()
        {
            // create a dummy zip file
            var archivePath = PathHelper.CreateTemporaryFilePath("files");
            using (var fs = File.Open(archivePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    // create some random files
                    var txt = zip.CreateEntry("hello.txt");
                    using (var txtStream = txt.Open())
                    {
                        var buf = Encoding.UTF8.GetBytes("Hello world!");
                        await txtStream.WriteAsync(buf, 0, buf.Length);
                    }
                    var html = zip.CreateEntry("hello.html");
                    using (var htmlStream = html.Open())
                    {
                        var buf = Encoding.UTF8.GetBytes("<html><body><h1>Hello world!</h1></body></html>");
                        await htmlStream.WriteAsync(buf, 0, buf.Length);
                    }

                    zip.CreateEntry("other.html");
                }
            }

            // upload the file, without moving it (the file is copied to cache instead)
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var file = await client.PutFile(archivePath, putIntoCacheMove: false);
            Console.WriteLine($"Put file:\n\tStatus: {file.Status}\n\tFile Id: {file.S64Sha256}");
            
            // try uploading the same file, again
            var file2 = await client.PutFile(archivePath);
            Console.WriteLine($"Put file:\n\tStatus: {file2.Status}\n\tFile Id: {file2.S64Sha256}");

            // unzip all .html files
            var unzipped = await client.UnzipFile(file2.S64Sha256, @"\.html$");
            Console.WriteLine("Unzipped: {0}", string.Join(", ", unzipped.ExtractedFiles.Select(t => t.Key)));

            // delete the archive
            var delete = await client.DeleteFile(file.S64Sha256);

            var query = await client.QueryFile(file.S64Sha256);
            Console.WriteLine($"File exists: {query.FileExists}");

            // select an html file
            var toShare = unzipped.ExtractedFiles.First();

            // create a file share that is viewable 4 times
            var share = await client.ShareFile(new Common.Structures.FileShare(toShare.Value.S64Sha256, "index.html", 4));
            Console.WriteLine($"Share url: {share.ShareUrl}");

            // the archive should remain in the cache
            var download = await client.GetFile(file.S64Sha256);
            Console.WriteLine($"From cache: {download}");
            sw.Stop();
            Console.WriteLine($"Took {sw.ElapsedMilliseconds:0.000}ms");
        }
    }
}
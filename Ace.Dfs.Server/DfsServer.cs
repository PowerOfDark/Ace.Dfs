using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Ace.Dfs.Common;
using Ace.Dfs.Common.Packets;
using Ace.Dfs.Common.Packets.DeleteFile;
using Ace.Dfs.Common.Packets.DownloadFile;
using Ace.Dfs.Common.Packets.Ping;
using Ace.Dfs.Common.Packets.PutFile;
using Ace.Dfs.Common.Packets.QueryFile;
using Ace.Dfs.Common.Packets.ShareFile;
using Ace.Dfs.Common.Packets.UnzipFile;
using Ace.Dfs.Server.Helpers;
using Ace.Dfs.Server.SendFile;
using Ace.Dfs.Server.Structures;
using Ace.Dfs.Server.UnzipFile;
using Ace.Dfs.Server.VerifyFile;
using Ace.Networking;
using Ace.Networking.Handlers;
using Ace.Networking.Threading;

namespace Ace.Dfs.Server
{
    public static class DfsServer
    {
        public static readonly ConcurrentDictionary<int, IncomingFileStatus> IncomingFiles =
            new ConcurrentDictionary<int, IncomingFileStatus>();

        //public static readonly ConcurrentDictionary<long, Connection> Shards = new ConcurrentDictionary<long, Connection>();
        //public static readonly ConcurrentDictionary<long, Connection> Realms = new ConcurrentDictionary<long, Connection>();
        private static volatile bool _running;

        public static ThreadedQueueProcessor<IncomingFileStatus> VerifyQueue { get; private set; }
        public static ThreadedQueueProcessor<SendFileItem> SendQueue { get; private set; }
        public static ThreadedQueueProcessor<UnzipFileItem> UnzipQueue { get; private set; }

        public static TcpServer Server { get; private set; }

        public static void Start()
        {
            if (_running)
            {
                return;
            }
            var cfg = DfsProtocolConfiguration.Instance;
            var cert = new X509Certificate2(Config.Dfs.CertificatePath, Config.Dfs.CertificatePassword);
            var factory = cfg.GetServerSslFactory(cert);

            VerifyQueue = new ThreadedQueueProcessor<IncomingFileStatus>(Config.Dfs.VerifyQueue, new VerifyFileWorker());

            SendQueue = new ThreadedQueueProcessor<SendFileItem>(Config.Dfs.SendQueue, new SendFileWorker());

            UnzipQueue = new ThreadedQueueProcessor<UnzipFileItem>(Config.Dfs.UnzipQueue, new UnzipFileWorker());

            Server = new TcpServer(new IPEndPoint(IPAddress.Any, Config.Dfs.Port), cfg, factory);
            Server.ReceiveTimeout = TimeSpan.FromMilliseconds(Config.Dfs.PingInterval);
            Server.Timeout += Server_Timeout;
            Server.IdleTimeout += Server_IdleTimeout;
            Server.AcceptClient = AcceptClient;
            Server.On<C2S_FileShareRequest>(HandleFileShareRequest);
            Server.On<C2S_FileInfoRequest>(HandleFileQuery);
            Server.On<C2S_PutFileRequest>(HandlePutFileRequest);
            Server.On<C2S_DownloadFileRequest>(HandleDownloadFileRequest);
            Server.OnRequest<C2S_DownloadFileReady>(CaptureDownloadFileReady);
            Server.On<C2S_DeleteFileRequest>(HandleDeleteFileRequest);
            Server.OnRequest<C2S_UnzipFileRequest>(CaptureUnzipFileRequest);
            Server.ClientDisconnected += Server_ClientDisconnected;
            Server.Start();
            _running = true;
        }

        private static void CaptureUnzipFileRequest(RequestWrapper request)
        {
            var req = request.Request as C2S_UnzipFileRequest;
            string path = null;
            if (req == null || !FileManager.FileExists(req.S64Sha256, out path) || !request.Connection.Authorized(DfsPermissions.UnzipFile))
            {
                request.TrySendResponse(new S2C_UnzipFileResult(null, false, false), out _);
                return;
            }


            FileStream fs;
            try
            {
                fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            }
            catch
            {
                request.TrySendResponse(new S2C_UnzipFileResult(null, false, false), out _);
                return;
            }
            UnzipQueue.Enqueue(new UnzipFileItem {FileStream = fs, Request = request},
                CryptoHelper.BuildDiscriminatorFromS64(req.S64Sha256));
        }

        private static object HandleDeleteFileRequest(Connection connection, C2S_DeleteFileRequest payload)
        {
            if (!connection.Authorized(DfsPermissions.DeleteFile))
            {
                return new S2C_DeleteFileResult(false);
            }
            return new S2C_DeleteFileResult(FileManager.Delete(payload.S64Sha256));
        }

        private static void Server_IdleTimeout(Connection connection)
        {
            connection.SendRequest<S2C_Ping, C2S_Ping>(S2C_Ping.Instance, TimeSpan.FromMilliseconds(Config.Dfs.PingInterval / 2))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted || t.IsCanceled)
                    {
                        connection.Close();
                    }
                });
        }

        private static void CaptureDownloadFileReady(RequestWrapper request)
        {
            var connection = request.Connection;
            var payload = request.Request as C2S_DownloadFileReady;
            if (payload == null || !connection.Authorized(DfsPermissions.DownloadFile))
            {
                request.TrySendResponse(S2C_DownloadFileResult.Failed, out _);
                return;
            }
            var fileId = connection.Data.Get<string>($"F_{payload?.BufferId}", null);
            if (fileId == null)
            {
                request.TrySendResponse(S2C_DownloadFileResult.Failed, out _);
                return;
            }
            connection.Data[$"F_{payload.BufferId}"] = null;
            var path = PathHelper.MapLocal(fileId, Config.Dfs.Path);
            SendQueue.Enqueue(new SendFileItem
            {
                FileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete),
                Request = request,
                BufferId = payload.BufferId
            }, (int) connection.Identifier + payload.BufferId);
        }

        private static object HandleDownloadFileRequest(Connection connection, C2S_DownloadFileRequest payload)
        {
            if (!connection.Authorized(DfsPermissions.DownloadFile))
            {
                return new S2C_DownloadFileReady(null);
            }
            var fi = FileManager.GetFileInfo(payload.S64Sha256);
            if (fi?.FileExists ?? false)
            {
                connection.Data[$"F_{payload.BufferId}"] = payload.S64Sha256;
            }
            return new S2C_DownloadFileReady(fi);
        }


        private static void Server_Timeout()
        {
            var now = DateTime.Now;
            foreach (var kv in IncomingFiles)
            {
                lock (kv.Value)
                {
                    if ((now - kv.Value.LastPart).TotalMilliseconds > Config.Dfs.FileTransferTimeout ||
                        !kv.Value.Connection.Connected)
                    {
                        kv.Value.Connection.DestroyRawDataBuffer(kv.Key);
                        IncomingFiles.TryRemove(kv.Key, out _);
                        var name = kv.Value.FileStream.Name;
                        kv.Value.FileStream?.Dispose();
                        try
                        {
                            File.Delete(name);
                        }
                        catch
                        {
                        }
                        if (kv.Value.Connection.Connected)
                        {
                            kv.Value.Connection.Send(new S2C_PutFileComplete(kv.Value.Request.S64Sha256, PutFileStatus.Error));
                        }
                    }
                }
            }
        }

        private static object HandlePutFileRequest(Connection connection, C2S_PutFileRequest payload)
        {
            if (!connection.Authorized(DfsPermissions.PutFile))
            {
                return new S2C_PutFileReady(PutFileStatus.Error);
            }
            if (FileManager.FileExists(payload.S64Sha256))
            {
                return new S2C_PutFileReady(PutFileStatus.FileExists);
            }
            var bufId = connection.CreateNewRawDataBuffer();
            connection.AppendIncomingRawDataHandler(bufId, HandleRawDataTransfer);
            var status = new IncomingFileStatus
            {
                Connection = connection,
                Request = payload,
                FileStream = PathHelper.CreateTemporaryFile(Config.Dfs.Path),
                LastPart = DateTime.Now
            };
            if (!IncomingFiles.TryAdd(bufId, status))
            {
                connection.DestroyRawDataBuffer(bufId);
                status.FileStream.Dispose();
                return new S2C_PutFileReady(PutFileStatus.Error);
            }
            return new S2C_PutFileReady(bufId);
        }

        private static object HandleRawDataTransfer(int bufferId, int seq, Stream stream)
        {
            if (!IncomingFiles.TryGetValue(bufferId, out var status))
            {
                return null;
            }

            lock (status)
            {
                var allParts = status.Request.ContentLength / DfsConfiguration.ChunkSize +
                               (status.Request.ContentLength % DfsConfiguration.ChunkSize == 0 ? 0 : 1);
                if (status.FileStream.Position != seq * DfsConfiguration.ChunkSize)
                {
                    //Console.WriteLine("Warning: out of order file transfer");
                    status.FileStream.Seek(seq * DfsConfiguration.ChunkSize, SeekOrigin.Begin);
                }
                stream.CopyTo(status.FileStream);
                if (Interlocked.Increment(ref status.DownloadedParts) == allParts)
                {
                    //?
                    status.Connection.DestroyRawDataBuffer(bufferId);
                    IncomingFiles.TryRemove(bufferId, out _);
                    VerifyQueue.Enqueue(status, CryptoHelper.BuildDiscriminatorFromS64(status.Request.S64Sha256));
                }
            }

            return null;
        }

        private static object HandleFileQuery(Connection connection, C2S_FileInfoRequest payload)
        {
            // every authed client can do that
            if (!connection.Authorized(DfsPermissions.QueryFile))
            {
                return null;
            }
            return new S2C_FileInfoResult(FileManager.GetFileInfo(payload.FileId));
        }

        private static object HandleFileShareRequest(Connection connection, C2S_FileShareRequest payload)
        {
            // allow only realms
            if (!connection.Authorized(DfsPermissions.ShareFile))
            {
                return null;
            }
            return new S2C_FileShareResult(FileManager.Share(payload.Share));
        }

        private static void Server_ClientDisconnected(Connection connection, Exception exception)
        {
            var role = connection.Data.Get<string>("_R", null) != null;
            if (role)
            {
                VerifyQueue.RemoveClient();
                SendQueue.RemoveClient();
                UnzipQueue.RemoveClient();

                //var targetDict = role == DfsClientType.Shard ? Shards : Realms;
                //targetDict.TryRemove(connection.Identifier, out _);
            }
        }

        private static bool AcceptClient(Connection client)
        {
            if (client.SslCertificates.RemotePolicyErrors != SslPolicyErrors.None)
            {
                return false;
            }
            if (!PermissionManager.Authorize(client))
            {
                return false;
            }

            VerifyQueue.NewClient();
            SendQueue.NewClient();
            UnzipQueue.NewClient();

            //targetDict.TryAdd(client.Identifier, client);
            //client.Data["Role"] = type.ToString();
            return true;
        }
    }
}
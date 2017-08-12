using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ace.Dfs.Common;
using Ace.Dfs.Common.Packets;
using Ace.Dfs.Common.Packets.DeleteFile;
using Ace.Dfs.Common.Packets.DownloadFile;
using Ace.Dfs.Common.Packets.Ping;
using Ace.Dfs.Common.Packets.PutFile;
using Ace.Dfs.Common.Packets.QueryFile;
using Ace.Dfs.Common.Packets.ShareFile;
using Ace.Dfs.Common.Packets.UnzipFile;
using Ace.Networking;
using Ace.Networking.Helpers;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.Threading;

namespace Ace.Dfs.Client
{
    public class DfsClient
    {
        public readonly ConcurrentDictionary<int, IncomingFileStatus> IncomingFiles = new ConcurrentDictionary<int, IncomingFileStatus>();

        public readonly ConcurrentDictionary<string, TaskCompletionSource<string>> IncomingFilesTask =
            new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        private long _currentCacheSize;
        private volatile bool _initialized;

        public DfsClient(ISslStreamFactory sslFactory, TcpClient client, string path = "",
            long cacheSize = 1024 * 1024 * 1024 * 2L, int cacheLookupSize = 100_000, long minFree = 1024 * 1024 * 50)
        {
            var cfg = DfsProtocolConfiguration.Instance;
            Connection = new Connection(client, cfg, sslFactory);
            Path = path;
            CacheLookupSize = cacheLookupSize;
            Lookup = new SortedSet<Tuple<DateTime, string>>();
            MinFree = minFree;
            CacheSize = cacheSize;
        }

        public ThreadedQueueProcessor<VerifyFileItem> VerifyQueue { get; protected set; }
        public long MinFree { get; }

        public string Path { get; }
        public long CacheSize { get; set; }
        public long CurrentCacheSize => _currentCacheSize;
        public int CacheLookupSize { get; }

        public SortedSet<Tuple<DateTime, string>> Lookup { get; }

        public Connection Connection { get; }

        public void Initialize(ThreadedQueueProcessorParameters verifyQueue = null)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("Already initialized");
            }
            _initialized = true;
            Connection.Initialize();
            Connection.On<S2C_Ping>(HandlePing);
            Connection.Disconnected += Connection_Disconnected;
            VerifyQueue = new ThreadedQueueProcessor<VerifyFileItem>(verifyQueue ?? new ThreadedQueueProcessorParameters
            {
                MaxThreads = 4,
                MinThreads = 4,
                MaxThreadsPerClient = null,
                QueueCapacity = 100
            }, new VerifyFileWorker());
            VerifyQueue.Initialize();

            PathHelper.ClearTempDirectory(Path);

            ScanCache();
            ClearCache();
        }

        private object HandlePing(Connection connection, S2C_Ping payload)
        {
            return C2S_Ping.Instance;
        }

        private void Connection_Disconnected(Connection connection, Exception exception)
        {
            VerifyQueue.Stop();
        }

        private void ScanCache()
        {
            var dir = new DirectoryInfo(Path);
            if (!dir.Exists)
            {
                dir.Create();
                return;
            }
            var files = dir.EnumerateFiles("*", SearchOption.AllDirectories);
            long size = 0;
            lock (Lookup)
            {
                Lookup.Clear();
                var count = 0;
                foreach (var file in files)
                {
                    var date = file.LastAccessTime;
                    var val = PathHelper.MapHandleFromLocal(file.FullName);
                    if (count < CacheLookupSize)
                    {
                        if (Lookup.Add(new Tuple<DateTime, string>(date, val)))
                        {
                            count++;
                        }
                    }
                    else
                    {
                        var max = Lookup.Max;
                        if (date < max.Item1)
                        {
                            if (Lookup.Add(new Tuple<DateTime, string>(date, val)))
                            {
                                count++;
                                Lookup.Remove(max);
                            }
                        }
                    }
                    size += file.Length;
                }
            }
            _currentCacheSize = size;
        }

        public void ClearCache(long minFree = -1)
        {
            if (minFree < 0)
            {
                minFree = MinFree;
            }
            var scans = 0;
            while (_currentCacheSize > CacheSize - minFree)
            {
                lock (Lookup)
                {
                    var it = Lookup.GetEnumerator();
                    while (it.MoveNext())
                    {
                        var kv = it.Current;
                        var f = new FileInfo(PathHelper.MapLocal(kv.Item2, Path));
                        if (f.LastAccessTime > kv.Item1)
                        {
                            Lookup.Remove(kv);
                        }
                        else
                        {
                            try
                            {
                                f.Delete();
                                var nv = Interlocked.Add(ref _currentCacheSize, -f.Length);
                                if (nv < CacheSize - minFree)
                                {
                                    break;
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    it.Dispose();
                }
                if (scans == 0 && Lookup.Count == 0)
                {
                    ScanCache();
                    scans++;
                }
                else
                {
                    return;
                }
            }
        }

        public Task<S2C_FileInfoResult> QueryFile(string fileId, TimeSpan? timeout = null)
        {
            return Connection.SendRequest<C2S_FileInfoRequest, S2C_FileInfoResult>(new C2S_FileInfoRequest(fileId), timeout);
        }

        public Task<S2C_FileShareResult> ShareFile(Common.Structures.FileShare share, TimeSpan? timeout = null)
        {
            return Connection.SendRequest<C2S_FileShareRequest, S2C_FileShareResult>(new C2S_FileShareRequest(share), timeout);
        }

        public async Task<S2C_PutFileComplete> PutFile(string path, bool putIntoCache = true, bool putIntoCacheMove = true)
        {
            if (!File.Exists(path))
            {
                throw new ArgumentException(nameof(path));
            }
            Task<object> completion;
            string handle;

            void Complete(S2C_PutFileComplete res)
            {
                if (res.Status != PutFileStatus.Error && putIntoCache)
                {
                    PutIntoCache(path, handle, putIntoCacheMove);
                }
            }

            using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                handle = fs.S64Sha256L();
                fs.Seek(0, SeekOrigin.Begin);
                var len = fs.Length;

                var req = new C2S_PutFileRequest(handle, len);
                var ready = await Connection.SendRequest<C2S_PutFileRequest, S2C_PutFileReady>(req, TimeSpan.FromSeconds(10000));
                if (ready.Status == PutFileStatus.FileExists)
                {
                    var res = new S2C_PutFileComplete(handle, PutFileStatus.FileExists);
                    Complete(res);
                    return res;
                }
                if (ready.Status == PutFileStatus.Error)
                {
                    throw new Exception(ready.Status.ToString());
                }
                completion = Connection.Receive((o, t) => o is S2C_PutFileComplete c && c.S64Sha256 == handle);
                var allParts = DfsConfiguration.GetNumberOfChunks(len);
                var left = len;
                for (var i = 0; i < allParts; i++)
                {
                    var completed = completion.IsCompleted;
                    if (completed)
                    {
                        if (((S2C_PutFileComplete)completion.Result).Status != PutFileStatus.Error)
                            break;
                    }
                    if (completed || completion.IsFaulted)
                    {
                        throw new Exception("Server failed", completion.Exception);
                    }
                    var toSend = Math.Min(DfsConfiguration.ChunkSize, left);
                    await Connection.EnqueueSendRaw(ready.BufferId, i, fs, (int) toSend, false);
                    left -= toSend;
                }
            }
            var result = (S2C_PutFileComplete) await completion;
            Complete(result);
            return result;
        }

        internal void PutIntoCache(string path, string s64Sha256 = null, bool move = true, bool clear = true)
        {
            if (s64Sha256 == null)
            {
                using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    s64Sha256 = fs.S64Sha256L();
                }
            }
            if (PathHelper.FileExists(s64Sha256, out var targetPath, Path))
            {
                return;
            }
            var len = new FileInfo(path).Length;
            new FileInfo(targetPath).Directory.Create();
            if (move)
            {
                try
                {
                    File.Move(path, targetPath);
                    Interlocked.Add(ref _currentCacheSize, len);
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    File.Copy(path, targetPath, false);
                    Interlocked.Add(ref _currentCacheSize, len);
                }
                catch
                {
                }
            }
            if (clear)
            {
                ClearCache();
            }
        }

        /// <summary>
        ///     Downloads a remote file, if it succeeds, the local path to the file is returned
        /// </summary>
        public async Task<string> DownloadFile(string s64Sha256, bool verifyHash = true, TimeSpan? timeout = null)
        {
            var completion = TaskHelper.CreateTaskCompletionSource<string>(s64Sha256, timeout);
            if (!IncomingFilesTask.TryAdd(s64Sha256, completion))
            {
                if (!IncomingFilesTask.TryGetValue(s64Sha256, out var tcs))
                {
                    throw new Exception("what?");
                }
                return await tcs.Task;
            }
            var bufId = Connection.CreateNewRawDataBuffer();
            Connection.AppendIncomingRawDataHandler(bufId, HandleRawDataTransfer);

            FileStream tmp = null;
            Task<S2C_DownloadFileResult> serverTask = null;

            void CleanUp()
            {
                var name = tmp?.Name;
                tmp?.Dispose();
                if (name != null)
                {
                    try
                    {
                        File.Delete(name);
                    }
                    catch
                    {
                    }
                }
                Connection.DestroyRawDataBuffer(bufId);
                IncomingFiles.TryRemove(bufId, out _);
            }

            try
            {
                tmp = PathHelper.CreateTemporaryFile(Path);
                var req = new C2S_DownloadFileRequest(bufId, s64Sha256);
                var res = await Connection.SendRequest<C2S_DownloadFileRequest, S2C_DownloadFileReady>(req, timeout);
                if (!res.Success)
                {
                    throw new Exception("File not available");
                }
                var status = new IncomingFileStatus
                {
                    TaskCompletionSource = completion,
                    FileInfo = res.FileInfo,
                    FileStream = tmp,
                    VerifyHash = verifyHash
                };

                if (!IncomingFiles.TryAdd(bufId, status))
                {
                    throw new Exception("Dictionary error");
                }

                serverTask =
                    Connection.SendRequest<C2S_DownloadFileReady, S2C_DownloadFileResult>(new C2S_DownloadFileReady(bufId), timeout);
            }
            catch (Exception e)
            {
                CleanUp();
                if (IncomingFilesTask.TryRemove(s64Sha256, out var tcs))
                {
                    tcs?.TrySetException(e);
                }
                throw new Exception("Error while downloading the file", e);
            }

            var done = await Task.WhenAny(serverTask, completion.Task);
            if (serverTask.IsCompleted && !serverTask.Result.Success)
            {
                completion.TrySetException(new Exception("Server error"));
            }


            return await completion.Task.ContinueWith(t =>
            {
                //Console.WriteLine("Removing task..");
                CleanUp();
                IncomingFilesTask.TryRemove((string) t.AsyncState, out _);
                return t.Result;
            });
        }

        private object HandleRawDataTransfer(int bufferId, int seq, Stream stream)
        {
            if (!IncomingFiles.TryGetValue(bufferId, out var status))
            {
                return null;
            }
            lock (status)
            {
                var allParts = DfsConfiguration.GetNumberOfChunks(status.FileInfo.ContentLength);
                if (status.FileStream.Position != seq * DfsConfiguration.ChunkSize)
                {
                    //Console.WriteLine("Warning: out of order file transfer");
                    status.FileStream.Seek(seq * DfsConfiguration.ChunkSize, SeekOrigin.Begin);
                }
                stream.CopyTo(status.FileStream);
                if (Interlocked.Increment(ref status.DownloadedParts) == allParts)
                {
                    VerifyQueue.Enqueue(new VerifyFileItem {Client = this, Status = status},
                        CryptoHelper.BuildDiscriminatorFromS64((string) status.TaskCompletionSource.Task.AsyncState));
                }
            }
            return null;
        }

        /// <summary>
        ///     Tries to retrieve the specified file from cache,
        ///     if it fails, it calls <see cref="DownloadFile(string, TimeSpan?)" />
        /// </summary>
        /// <param name="s64Sha256">S64Sha256 hash of the file</param>
        /// <param name="fromCache">Whether to retrieve file from cache</param>
        /// <param name="downloadVerifyHash">Whether to verify the file after downloading it</param>
        /// <param name="timeout">Timeout for the operation</param>
        /// <returns>Absolute local path to the downloaded file.</returns>
        public Task<string> GetFile(string s64Sha256, bool fromCache = true, bool downloadVerifyHash = true, TimeSpan? timeout = null)
        {
            if (fromCache)
            {
                if (PathHelper.FileExists(s64Sha256, out var path, Path))
                {
                    File.SetLastAccessTime(path, DateTime.Now);
                    return Task.FromResult(path);
                }
            }
            return DownloadFile(s64Sha256, downloadVerifyHash, timeout);
        }

        public Task<S2C_DeleteFileResult> DeleteFile(string s64Sha256, TimeSpan? timeout = null)
        {
            return Connection.SendRequest<C2S_DeleteFileRequest, S2C_DeleteFileResult>(new C2S_DeleteFileRequest(s64Sha256), timeout);
        }

        public Task<S2C_UnzipFileResult> UnzipFile(string s64Sha256, string regexFilter = null, TimeSpan? timeout = null)
        {
            if (regexFilter != null)
            {
                new Regex(regexFilter); // verify that the filter is ok
            }
            return Connection.SendRequest<C2S_UnzipFileRequest, S2C_UnzipFileResult>(new C2S_UnzipFileRequest(s64Sha256, regexFilter),
                timeout);
        }
    }
}
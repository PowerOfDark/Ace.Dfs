using System;
using System.IO;
using Ace.Dfs.Common;
using Ace.Networking.Interfaces;

namespace Ace.Dfs.Client
{
    public class VerifyFileWorker : IWorker<VerifyFileItem>
    {
        public void DoWork(VerifyFileItem item)
        {
            if (item.Status.TaskCompletionSource.Task.IsCompleted)
            {
                return;
            }

            var error = item.Status.FileStream.Length != item.Status.FileInfo.ContentLength;
            if (!error)
            {
                var reqHash = (string) item.Status.TaskCompletionSource.Task.AsyncState;
                if (item.Status.VerifyHash)
                {
                    item.Status.FileStream.Seek(0, SeekOrigin.Begin);
                    var s64Sha256 = item.Status.FileStream.S64Sha256();
                    if (s64Sha256 != reqHash)
                    {
                        error = true;
                    }
                }
                if (!error)
                {
                    var name = item.Status.FileStream.Name;
                    item.Status.FileStream.Dispose();
                    try
                    {
                        item.Client.PutIntoCache(name, reqHash, true, false);
                        //FileManager.PutLocal(item.Local.FullName, s64Sha256, true);
                    }
                    catch (Exception e)
                    {
                        error = !PathHelper.FileExists(reqHash, item.Client.Path);
                    }
                }
            }

            if (error)
            {
                item.Status.TaskCompletionSource.TrySetException(new Exception("Could not verify the file."));
            }
            else
            {
                item.Status.TaskCompletionSource.TrySetResult(
                    PathHelper.MapLocal((string) item.Status.TaskCompletionSource.Task.AsyncState, item.Client.Path));
            }
        }
    }
}
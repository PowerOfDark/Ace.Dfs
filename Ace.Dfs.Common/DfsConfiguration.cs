namespace Ace.Dfs.Common
{
    public static class DfsConfiguration
    {
        public const int ChunkSize = 512 * 1024;

        public static int GetNumberOfChunks(long length)
        {
            return (int) (length / ChunkSize + (length % ChunkSize == 0 ? 0 : 1));
        }
    }
}
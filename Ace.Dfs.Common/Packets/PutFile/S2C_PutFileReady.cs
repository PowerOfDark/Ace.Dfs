using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Dfs.Common.Packets.PutFile
{
    [ProtoContract]
    [Guid("8F9E8471-EDDA-4B75-B160-7E824EAED886")]
    public enum PutFileStatus
    {
        Error = 0,
        FileExists = 1,
        Ready = 2,
        Complete = 4
    }

    [ProtoContract]
    [Guid("8341F281-053D-4BD5-9BBA-311629F56D1F")]
    public class S2C_PutFileReady
    {
        public S2C_PutFileReady(PutFileStatus status)
        {
            Status = status;
        }

        public S2C_PutFileReady(int bufferId) : this(PutFileStatus.Ready)
        {
            BufferId = bufferId;
        }

        protected S2C_PutFileReady()
        {
        }

        [ProtoMember(1)]
        public PutFileStatus Status { get; protected set; }

        [ProtoMember(2)]
        public int BufferId { get; protected set; }
    }
}
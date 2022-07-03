using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interface
{
    public interface IClient
    {
        Task Send(IPacket message, CancellationToken cancellationToken = default);
        Task<IPacket> Receive(CancellationToken cancellationToken = default);
        Task Connect(CancellationToken cancellationToken = default);
        void Disconnect();
        public bool IsReady { get; }
        public string Cid { get; }
        public string Info { get; }
    }
}

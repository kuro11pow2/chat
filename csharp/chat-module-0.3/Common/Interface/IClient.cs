using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interface
{
    public interface IClient
    {
        Task Send(IMessage message);
        Task<IMessage> Receive();
        Task Connect();
        void Disconnect();
        public bool IsConnected { get; }
        public string Cid { get; }
        public string Info { get; }
    }
}

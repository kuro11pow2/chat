using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interface
{
    public interface IClient
    {
        Task<string> Receive();
        Task Send(string message);
        Task Connect();
        void Disconnect();
        public bool IsConnected();
        public string GetInfo();
        public string GetCid();
        public int GetReceivedByteSize();
    }
}

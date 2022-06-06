using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interface
{
    public interface IClient
    {
        Task Receive();
        Task Send(string message);
        Task Connect();
        void Disconnect();
    }
}

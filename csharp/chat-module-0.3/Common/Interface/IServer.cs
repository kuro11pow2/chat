using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interface
{
    public interface IServer
    {
        Task<IClient> Accept();
        Task Kick(IClient client);
        Task Broadcast(IClient src, IMessage message);
        public string GetInfo();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interface
{
    public interface IRoom
    {
        Task<IClient> Accept();
        Task Kick(IClient client);
        Task Broadcast(IClient src, IMessage message);
        public string Rid { get; }
        public string Info { get; }
        public int UserCount { get; }
    }
}

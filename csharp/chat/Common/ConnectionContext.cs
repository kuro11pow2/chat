using System.Net.Sockets;

namespace Common
{
    public class ConnectionContext
    {
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public string Cid { get; set; }
        public bool IsConnected { get; set; }

        public ConnectionContext(TcpClient client, NetworkStream stream, string cid)
        {
            Client = client;
            Stream = stream;
            Cid = cid;
            IsConnected = true;
        }

        public override string ToString()
        {
            return $" [{nameof(ConnectionContext)}]\n{nameof(Cid)}: {Cid}\n{nameof(IsConnected)}: {IsConnected}";
        }
    }
}

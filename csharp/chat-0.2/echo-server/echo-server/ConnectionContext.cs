using System.Net.Sockets;


namespace chat
{
    class ConnectionContext
    {
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public string Cid { get; set; }
        public bool isConnected { get; set; }
        public ReceiveContext ReceiveContext { get; set; }

        public ConnectionContext(TcpClient client, NetworkStream stream, string cid)
        {
            Client = client;
            Stream = stream;
            Cid = cid;
            isConnected = true;
            ReceiveContext = new ReceiveContext();
        }

        public void Release()
        {
            Stream.Close();
            Client.Close();
            isConnected = false;
        }

        public override string ToString()
        {
            return $"{nameof(Cid)}: {Cid}\n{nameof(isConnected)}: {isConnected}\n{nameof(ReceiveContext)}: {ReceiveContext}";
        }
    }
}

using System.Net;
using System.Net.Sockets;

namespace Common
{
    using Utility;

    public interface IConnectionContext
    {
        Task Connect();
        void Close();
        ValueTask WriteAsync(ReadOnlyMemory<byte> buffer);
        Task<int> ReadAsync(byte[] buffer, int offset, int count);
        string ConnectionId { get; }
        string RemoteAddress { get; }
        int Port { get; }
        bool IsReady { get; set; }
    }

    public class ConnectionContext : IConnectionContext
    {
        private TcpClient? Client;
        private NetworkStream? Stream;
        private string _connectionId = "NA";

        public string ConnectionId { get { return _connectionId; } }
        public string RemoteAddress { get; }
        public int Port { get; }
        public bool IsReady { get; set; }

        public ConnectionContext(string remoteAddress, int port)
        {
            RemoteAddress = remoteAddress;
            Port = port;
        }

        public ConnectionContext(TcpClient client)
        {
            Client = client;
            IPEndPoint? remoteEndPoint = (IPEndPoint?)client.Client.RemoteEndPoint;

            if (remoteEndPoint == null)
                throw new Exception("TcpClient의 RemoteEndPoint가 null임");

            RemoteAddress = remoteEndPoint.Address.ToString();
            Port = remoteEndPoint.Port;

            Ready();
        }

        public async Task Connect()
        {
            if (IsReady == true)
            {
                Close();
                IsReady = false;
            }

            Client = await Task.Run(() =>
            {
                return new TcpClient(RemoteAddress, Port);
            });

            Ready();
        }

        private void Ready()
        {
            if (Client == null)
                throw new Exception("TcpiClient가 준비된 이후에 호출해야 함");
            Stream = Client.GetStream();
            _connectionId = $"{Client.Client.LocalEndPoint}-{Client.Client.RemoteEndPoint}";
            IsReady = true;

            Log.Print($"{ConnectionId} 연결 수립", LogLevel.OFF);
        }

        public void Close()
        {
            IsReady = false;
            if (Stream != null)
                Stream.Close();
            if (Client != null)
                Client.Close();
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer)
        {
            if (Stream == null || IsReady == false)
                throw new IOException("연결되어 있지 않음");
            return Stream.WriteAsync(buffer);
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            if (Stream == null || IsReady == false)
                throw new IOException("연결되어 있지 않음");
            return Stream.ReadAsync(buffer, offset, count);
        }


        public override string ToString()
        {
            return $" [{nameof(ConnectionContext)}]\n{nameof(ConnectionId)}: {ConnectionId}\n{nameof(IsReady)}: {IsReady}";
        }
    }
}

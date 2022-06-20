using System.Net.Sockets;

namespace Common
{
    public interface IConnectionContext
    {
        void Close();
        ValueTask WriteAsync(ReadOnlyMemory<byte> buffer);
        Task<int> ReadAsync(byte[] buffer, int offset, int count);
        string ConnectionId { get; }
        bool IsConnected { get; set; }
    }
    public class ConnectionContext : IConnectionContext
    {
        private TcpClient Client;
        private NetworkStream Stream;
        public string ConnectionId { get; }
        public bool IsConnected { get; set; }

        public ConnectionContext(TcpClient client, NetworkStream stream, string connectionId)
        {
            Client = client;
            Stream = stream;
            ConnectionId = connectionId;
            IsConnected = true;
        }

        public void Close()
        {
            Stream.Close();
            Client.Close();
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer)
        {
            return Stream.WriteAsync(buffer);
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            return Stream.ReadAsync(buffer, offset, count);
        }


        public override string ToString()
        {
            return $" [{nameof(ConnectionContext)}]\n{nameof(ConnectionId)}: {ConnectionId}\n{nameof(IsConnected)}: {IsConnected}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp
{
    class Client
    {
        public int Cid { get; }
        public string TargetAddress { get; }
        public int TargetPort { get; }

        private SocketHandler _socket { get; } = new SocketHandler();

        public string Context { get { return $"Client:{Cid}"; } }


        public Client(int cid, string targetAddress, int targetPort) : base()
        {
            Cid = cid;
            TargetAddress = targetAddress;
            TargetPort = targetPort;
        }
        public async Task Init()
        {
            await _socket.ConnectAsync(TargetAddress, TargetPort);
            Log.Print($"{Cid} 연결됨", ChatLogLevel.DEFAULT, $"{Context} {nameof(Init)}");
        }
        public async Task SendAsync(string message)
        {
            await _socket.SendAsync(message);
            Log.Print($"[{Cid}->server] \"{message}\"", ChatLogLevel.INFO, $"{Context} {nameof(SendAsync)}");
        }
        public async Task<string> ReceiveAsync()
        {
            string message = await _socket.ReceiveAsync();
            Log.Print($"[server->{Cid}] \"{message}\"", ChatLogLevel.INFO, $"{Context} {nameof(ReceiveAsync)}");
            return message;
        }
    }
}

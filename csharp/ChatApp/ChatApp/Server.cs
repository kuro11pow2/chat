using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Concurrent;


namespace ChatApp
{
    class Server
    {
        public int Port { get; }
        public int Backlog { get; }
        public int ConnectedClientCount { get; private set; }
        public int CurrentClientCount { get { return _clients.Count; } }
        private SocketHandler _listenSocket { get; } = new SocketHandler();
        private ConcurrentDictionary<int, SocketHandler> _clients { get; } = new ConcurrentDictionary<int, SocketHandler>();

        public string Context { get { return $"Server:{Port}"; } }

        public Server(int port, int backlog)
        {
            Port = port;
            Backlog = backlog;
        }
        public void Init()
        {
            _listenSocket.StartBindAndListen(Port, Backlog);
            Log.Print($"수신 시작", ChatLogLevel.RELEASE, $"{Context} {nameof(Init)}");
        }
        public async void AcceptAndRegister()
        {
            while (true)
            {
                SocketHandler client;
                int cid = ConnectedClientCount;

                try
                {
                    Socket socket = await _listenSocket.AcceptAsync();
                    client = new SocketHandler(socket);
                    _clients.TryAdd(cid, client);

                    Log.Print($"{cid} 등록됨", ChatLogLevel.RELEASE, $"{Context} {nameof(AcceptAndRegister)}");
                }
                catch
                {
                    continue;
                }

                ++ConnectedClientCount;
                ReceiveAndBroadcast(cid, client);
            }
        }
        private async void ReceiveAndBroadcast(int cid, SocketHandler client)
        {
            try
            {
                while (true)
                {
                    string message = await client.ReceiveAsync();
                    Log.Print($"[{cid}->server]: {message}", ChatLogLevel.DEFAULT, $"{Context} {nameof(ReceiveAndBroadcast)}");

                    List<Task> sendTasks = new List<Task>();

                    foreach (var item in _clients)
                    {
                        int targetId = item.Key;
                        SocketHandler target = item.Value;
                        sendTasks.Add(
                            Task.Run(async () => {
                                string realMessage = $"[{cid}->{targetId}]: {message}";
                                await target.SendAsync(realMessage);
                                Log.Print($"[server->{targetId}]: \"{realMessage}\"", ChatLogLevel.INFO, $"{Context} {nameof(ReceiveAndBroadcast)}");
                            }
                        ));
                    }

                    Task.WaitAll(sendTasks.ToArray());
                }
            }
            catch (SocketException ex)
            {
                SocketHandler tmp;
                _clients.TryRemove(cid, out tmp);
                tmp.Close();

                Log.Print($"{cid} 제거됨", ChatLogLevel.RELEASE, $"{Context} {nameof(ReceiveAndBroadcast)}");
            }
        }

        public string Status()
        {
            return $"{CurrentClientCount}/{ConnectedClientCount}";
        }
    }
}

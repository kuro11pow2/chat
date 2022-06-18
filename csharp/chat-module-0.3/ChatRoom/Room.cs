using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Concurrent;

using Common;
using Common.Utility;
using Common.Interface;
using System.Net;

namespace Chat
{
    public class Room : IRoom
    {
        private int Port;
        private ConcurrentDictionary<string, IClient> Clients = new ConcurrentDictionary<string, IClient>();

        private long SendMessageCount, ReceivedMessageCount;
        private long SendByteSize, ReceivedByteSize;

        private TcpListener listener;

        private string _rid;
        public string Rid { get { return _rid; } }

        private int _userCount;
        public int UserCount { get { return _userCount; } }

        public string Info { get { return $"{nameof(Rid)}: {Rid}\n{nameof(Port)}: {Port}\n{ nameof(UserCount)}: {UserCount}\n{nameof(SendMessageCount)}: {SendMessageCount}\n{nameof(ReceivedMessageCount)}: {ReceivedMessageCount}\n{nameof(SendByteSize)}: {SendByteSize}\n{nameof(ReceivedByteSize)}: {ReceivedByteSize}"; } }
    
        public Room(string rid, int port)
        {
            _rid = rid;
            Port = port;
            listener = new TcpListener(IPAddress.Any, Port);
        }

        public async Task Run()
        {
            listener.Start();

            while (true)
            {
                IClient client = await Accept();
                string cid = client.Cid;
                if (Clients.ContainsKey(cid))
                {
                    throw new Exception($"cid 중복: {cid}");
                }
                if (!Clients.TryAdd(cid, client))
                {
                    throw new Exception($"connection 등록 실패: {cid}");
                }

                Interlocked.Increment(ref _userCount);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (client.IsConnected)
                        {
                            Log.Print($"\n{client.Info}", LogLevel.DEBUG);
                            try
                            {
                                IMessage message = await client.Receive();
                                Interlocked.Increment(ref ReceivedMessageCount);
                                Interlocked.Add(ref ReceivedByteSize, message.GetFullBytesLength());

                                //await TcpClientUtility.SendEchoMessage(stream, context);
                                _ = Broadcast(client, message);
                            }
                            catch (ProtocolBufferOverflowException ex)
                            {
                                Log.Print($"{ex}", LogLevel.ERROR);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Print($"\n{ex}", LogLevel.ERROR);
                    }

                    Log.Print($"연결 종료", LogLevel.INFO);

                    string cid = client.Cid;

                    if (!Clients.TryRemove(cid, out IClient? tmpClient))
                    {
                        Log.Print($"{cid}를 Connections 에서 제외 실패", LogLevel.ERROR);
                    }

                    try
                    {
                        tmpClient?.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Log.Print($"{cid} 리소스 해제 실패\n{ex}", LogLevel.ERROR);
                    }
                    Log.Print($"{cid} connection 리소스 해제 완료", LogLevel.INFO);

                    Interlocked.Decrement(ref _userCount);
                });
            }
        }
        public async Task<IClient> Accept()
        {
            TcpClient tmpClient = await listener.AcceptTcpClientAsync();
            if (tmpClient.Client.RemoteEndPoint == null)
                throw new Exception("수락된 클라이언트의 RemoteEndPoint가 null임");
            Client client = new Client(tmpClient, (IPEndPoint)tmpClient.Client.RemoteEndPoint);
            return client;
        }

        public async Task Broadcast(IClient src, IMessage message)
        {
            List<Task> tasks = new List<Task>();
            long sendCount = Clients.Count;

            foreach (var pair in Clients)
            {
                IClient dst = pair.Value;
                tasks.Add(Task.Run(async () => { await dst.Send(message); }));
            }

            await Task.WhenAll(tasks);
            Interlocked.Add(ref SendMessageCount, sendCount);
            Interlocked.Add(ref SendByteSize, sendCount * message.GetFullBytesLength());

            return;
        }

        public Task Kick(IClient client)
        {
            throw new NotImplementedException();
        }

        public async Task RunMonitor()
        {
            while (true)
            {
                await Task.Delay(5000);
                Log.Print($"\n{Info}", LogLevel.OFF, "server monitor");
            }
        }
    }
}

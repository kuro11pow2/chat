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
    public class ChatRoom : IServer
    {
        private int Port;
        private ConcurrentDictionary<string, IClient> Clients = new ConcurrentDictionary<string, IClient>();

        private long SendMessageCount, ReceivedMessageCount;
        private long SendByteSize, ReceivedByteSize;

        private TcpListener listener;

        public ChatRoom(int port)
        {
            Port = port;
            listener = new TcpListener(IPAddress.Any, Port);
        }

        public async Task Run()
        {
            listener.Start();

            while (true)
            {
                IClient client = await Accept();
                string cid = client.GetCid();
                if (Clients.ContainsKey(cid))
                {
                    throw new Exception($"cid 중복: {cid}");
                }
                if (!Clients.TryAdd(cid, client))
                {
                    throw new Exception($"connection 등록 실패: {cid}");
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (client.IsConnected())
                        {
                            Log.Print($"\n{client.GetInfo()}", LogLevel.DEBUG);
                            try
                            {
                                string s = await client.Receive();
                                Interlocked.Increment(ref ReceivedMessageCount);
                                Interlocked.Add(ref ReceivedByteSize, client.GetReceivedByteSize());

                                //await TcpClientUtility.SendEchoMessage(stream, context);
                                _ = Broadcast(client, s);
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

                    string cid = client.GetCid();

                    if (!Clients.TryRemove(cid, out IClient? tmpClient))
                    {
                        Log.Print($"{cid}를 Connections 에서 제외 실패", LogLevel.ERROR);
                    }

                    try
                    {
                        tmpClient.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Log.Print($"{cid} 리소스 해제 실패\n{ex}", LogLevel.ERROR);
                        return;
                    }
                    Log.Print($"{cid} connection 리소스 해제 완료", LogLevel.INFO);
                });
            }
        }

        public async Task Broadcast(IClient src, string message)
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
            Interlocked.Add(ref SendByteSize, sendCount * src.GetReceivedByteSize());

            return;
        }

        public async Task RunMonitor()
        {
            while (true)
            {
                await Task.Delay(5000);
                Log.Print($"\nConnections.Count: {Clients.Count}\n{nameof(SendMessageCount)}: {SendMessageCount}\n{nameof(ReceivedMessageCount)}: {ReceivedMessageCount}\n{nameof(SendByteSize)}: {SendByteSize}\n{nameof(ReceivedByteSize)}: {ReceivedByteSize}", LogLevel.OFF, "server monitor");
            }
        }


        public async Task<IClient> Accept()
        {
            TcpClient tmpClient = await listener.AcceptTcpClientAsync();
            if (tmpClient.Client.RemoteEndPoint == null)
                throw new Exception("수락된 클라이언트의 RemoteEndPoint가 null임");
            ChatClient client = new ChatClient(tmpClient, (IPEndPoint)tmpClient.Client.RemoteEndPoint);
            return client;
        }

        public string GetInfo()
        {
            throw new NotImplementedException();
        }

        public Task Kick(IClient client)
        {
            throw new NotImplementedException();
        }
    }
}

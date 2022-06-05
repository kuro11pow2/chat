using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;


namespace chat
{
    class Server
    {
        private int Port;
        private ConcurrentDictionary<string, ConnectionContext> Connections = new ConcurrentDictionary<string, ConnectionContext>();

        private long SendMessageCount, ReceivedMessageCount;
        private long SendByteSize, ReceivedByteSize;

        public Server(int port)
        {
            Port = port;
        }

        public async Task Run()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            while (true)
            {
                TcpClient tc = await listener.AcceptTcpClientAsync();
                NetworkStream stream = tc.GetStream();
                ConnectionContext context = new ConnectionContext(tc, stream, $"{tc.Client.LocalEndPoint}-{tc.Client.RemoteEndPoint}");

                if (Connections.ContainsKey(context.Cid))
                {
                    throw new Exception($"cid 중복: {context.Cid}");
                }
                if (!Connections.TryAdd(context.Cid, context))
                {
                    throw new Exception($"connection 등록 실패: {context.Cid}");
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Proccess(context);
                    }
                    catch (Exception ex)
                    {
                        Log.Print($"\n{ex}", LogLevel.ERROR);
                    }

                    Log.Print($"연결 종료", LogLevel.INFO);

                    if (!Connections.TryRemove(context.Cid, out ConnectionContext retrievedValue))
                    {
                        Log.Print($"{context.Cid}를 Connections 에서 제외 실패", LogLevel.ERROR);
                    }

                    try
                    {
                        retrievedValue.Release();
                    }
                    catch (Exception ex)
                    {
                        Log.Print($"{retrievedValue.Cid} 리소스 해제 실패\n{ex}", LogLevel.ERROR);
                        return;
                    }
                    Log.Print($"{retrievedValue.Cid} connection 리소스 해제 완료", LogLevel.INFO);
                });
            }
        }

        private async Task Proccess(ConnectionContext context)
        {
            while (context.isConnected)
            {
                Log.Print($"\n{context}", LogLevel.DEBUG);
                try
                {
                    await TcpClientUtility.ReceiveMessage(context);
                    Interlocked.Increment(ref ReceivedMessageCount);
                    Interlocked.Add(ref ReceivedByteSize, context.ReceiveContext.sizeBytes.Length + context.ReceiveContext.expectedMessageBytesLength);

                    //await TcpClientUtility.SendEchoMessage(stream, context);
                    _ = SendMessage2All(context);
                }
                catch (ReceiveOverflowException ex)
                {
                    Log.Print($"{ex}", LogLevel.ERROR);
                }
            }
        }

        private async Task SendMessage2All(ConnectionContext src)
        {
            List<Task> tasks = new List<Task>();
            long sendCount = Connections.Count;

            foreach (var pair in Connections)
            {
                ConnectionContext dst = pair.Value;
                tasks.Add(TcpClientUtility.SendLastReceivedMessage(src, dst));
            }

            await Task.WhenAll(tasks);
            Interlocked.Add(ref SendMessageCount, sendCount);
            Interlocked.Add(ref SendByteSize, sendCount * (src.ReceiveContext.sizeBytes.Length + src.ReceiveContext.expectedMessageBytesLength));

            return;
        }

        public async Task RunMonitor()
        {
            while (true)
            {
                await Task.Delay(5000);
                Log.Print($"\nConnections.Count: {Connections.Count}\n{nameof(SendMessageCount)}: {SendMessageCount}\n{nameof(ReceivedMessageCount)}: {ReceivedMessageCount}\n{nameof(SendByteSize)}: {SendByteSize}\n{nameof(ReceivedByteSize)}: {ReceivedByteSize}", LogLevel.OFF, "server monitor");
            }
        }
    }
}

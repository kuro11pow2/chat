using System.Net.Sockets;
using System;
using System.Threading.Tasks;


namespace chat
{
    class Client
    {
        private string ServerAddress;
        private int Port;
        private int LocalId;

        public Client(string serverAddress, int port, int localId=-1)
        {
            ServerAddress = serverAddress;
            Port = port;
            LocalId = localId;
        }

        public async Task Run()
        {
            TcpClient tc = new TcpClient(ServerAddress, Port);
            NetworkStream stream = tc.GetStream();
            ConnectionContext context = new ConnectionContext(tc, stream, $"{tc.Client.RemoteEndPoint}-{tc.Client.LocalEndPoint}");

            Log.Print($"{context.Cid} 연결 수립", LogLevel.OFF);

            try
            { 
                await Proccess(context, TcpClientUtility.GetUserInput, 1000);
            }
            catch (Exception ex)
            {
                Log.Print($"\n{ex}", LogLevel.ERROR);
            }

            Log.Print($"{context.Cid} 연결 종료", LogLevel.INFO);

            try
            {
                context.Release();
            }
            catch (Exception ex)
            {
                Log.Print($"{context.Cid} 리소스 해제 실패\n{ex}", LogLevel.ERROR);
                return;
            }
            Log.Print($"{context.Cid} connection 리소스 해제 완료", LogLevel.INFO);
        }

        public async Task TestRun(int sendDelay)
        {
            TcpClient tc = await Task.Run(() =>
            {
                return new TcpClient(ServerAddress, Port);
            });
            NetworkStream stream = tc.GetStream();
            ConnectionContext context = new ConnectionContext(tc, stream, $"{tc.Client.RemoteEndPoint}-{tc.Client.LocalEndPoint}");

            Log.Print($"{context.Cid} 연결 수립", LogLevel.OFF);

            Func<string> getMessage = () =>
            {
                return $"{LocalId}";
            };

            try
            {
                await Proccess(context, getMessage, sendDelay);
            }
            catch (Exception ex)
            {
                Log.Print($"\n{ex}", LogLevel.ERROR);
            }

            Log.Print($"{context.Cid} 연결 종료", LogLevel.INFO);

            try
            {
                context.Release();
            }
            catch (Exception ex)
            {
                Log.Print($"{context.Cid} 리소스 해제 실패\n{ex}", LogLevel.ERROR);
                return;
            }
            Log.Print($"{context.Cid} connection 리소스 해제 완료", LogLevel.INFO);
        }

        private async Task Proccess(ConnectionContext context, Func<string> getMessage, int sendDelay)
        {
            _ = Task.Run(async () =>
            {
                while (context.isConnected)
                {
                    Log.Print($"\n{context}", LogLevel.DEBUG);
                    try
                    {
                        await TcpClientUtility.ReceiveMessage(context);
                        Log.Print($"received message: {context.ReceiveContext.messageStr}", LogLevel.INFO);
                    }
                    catch (ReceiveOverflowException ex)
                    {
                        Log.Print($"{ex}", LogLevel.ERROR);
                    }
                }
            });

            while (context.isConnected)
            {
                await Task.Delay(sendDelay);
                string message = getMessage();
                if (string.IsNullOrEmpty(message))
                    continue;
                try
                {
                    await TcpClientUtility.SendMessage(context, message);
                }
                catch (ReceiveOverflowException ex)
                {
                    Log.Print($"{ex}", LogLevel.ERROR);
                }
            }
        }
    }
}

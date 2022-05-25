using System.Net.Sockets;
using System.Net;
using System;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.IO;


namespace echo_server
{
    class PayloadEncoder
    {

        /// <summary>
        /// 메시지 크기 버퍼 최대 길이. 4 bytes 이하 (int 제약)
        /// </summary>
        public const int MAX_SIZE_BUFFER_LENGTH = 1;
        /// <summary>
        /// 메시지 버퍼 최대 길이. 2^(8 * MAX_SIZE_BUFFER_LENGTH)-1
        /// </summary>
        public static int MAX_MESSAGE_BUFFER_LENGTH = 1 << (8 * MAX_SIZE_BUFFER_LENGTH) - 1;

        public static int SizeBuffer2Num(byte[] sizeBuffer)
        {
            int ret = 0;
            for (int i = 0; i < sizeBuffer.Length; i++)
            {
                ret <<= 1;
                ret += sizeBuffer[i];
            }
            return ret;
        }

        public static byte[] Num2SizeBuffer(int sizeBufferLength)
        {
            return BitConverter.GetBytes(sizeBufferLength)[..MAX_SIZE_BUFFER_LENGTH];
        }

        public static string GetString(byte[] bytes, int index, int count)
        {
            return Encoding.UTF8.GetString(bytes, index, count);
        }

        public static byte[] GetBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

    }

    class Config
    {
        public LogLevel PrintLevel { get; set; }
        public int Port { get; set; }
    }

    class ReceiveContext
    {
        public bool isConnected { get; set; }
        public bool isOverflow { get; set; }
        public int messageLength { get; set; }
        public byte[] sizeBuffer { get; set; }
        public byte[] messageBuffer { get; set; }
        public string fullMessage { get; set; }

        public ReceiveContext()
        {
            isConnected = true;
            isOverflow = false;
            messageLength = 0;
            sizeBuffer = new byte[PayloadEncoder.MAX_SIZE_BUFFER_LENGTH];
            messageBuffer = new byte[PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH];
            fullMessage = "";
        }
    }

    class Server
    {
        

        TcpListener listener;

        public Server()
        {
        }

        public async Task Run()
        {
            listener = new TcpListener(IPAddress.Any, 7000);
            listener.Start();

            while (true)
            {
                TcpClient tc = await listener.AcceptTcpClientAsync();
                _ = Proccess(tc);
            }
        }

        private void Release(TcpClient tc, NetworkStream stream)
        {
            stream.Close();
            tc.Close();
        }


        private async Task ReceiveSize(NetworkStream stream, ReceiveContext context)
        {
            int receivedSizeBufferLength = 0;

            while (context.isConnected)
            {
                int currentReceived;

                currentReceived = await stream.ReadAsync(context.sizeBuffer, receivedSizeBufferLength, PayloadEncoder.MAX_SIZE_BUFFER_LENGTH - receivedSizeBufferLength);
                receivedSizeBufferLength += currentReceived;
                Log.Print($"sizeReceivedBuffLength: {receivedSizeBufferLength}");

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료");
                    context.isConnected = false;
                    continue;
                }
                else if (receivedSizeBufferLength < PayloadEncoder.MAX_SIZE_BUFFER_LENGTH)
                {
                    continue;
                }
                else if (receivedSizeBufferLength > PayloadEncoder.MAX_SIZE_BUFFER_LENGTH)
                {
                    Log.Print("메세지 사이즈 버퍼 크기를 초과하여 수신함");
                    context.isConnected = false;
                    continue;
                }

                context.messageLength = PayloadEncoder.SizeBuffer2Num(context.sizeBuffer);

                if (context.messageLength > PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH)
                {
                    Log.Print($"메시지 버퍼 크기를 초과하여 수신할 수 없음 : messageLength({context.messageLength}) > MESSAGE_BUFFER_LENGTH({PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH})");
                    context.isOverflow = true;
                }

                break;
            }
        }

        private async Task ReceiveMessage(NetworkStream stream, ReceiveContext context)
        {
            int receivedMessageBufferLength = 0;

            while (context.isConnected)
            {
                int currentReceived;

                if (context.isOverflow) // 메세지 버퍼 크기 초과하여 수신하려 한 케이스의 입력을 모두 제거
                {
                    int maxReceiveLength = Math.Min(PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH, context.messageLength - receivedMessageBufferLength);
                    currentReceived = await stream.ReadAsync(context.messageBuffer, 0, maxReceiveLength);
                    receivedMessageBufferLength += currentReceived;

                    Log.Print($"오버플로 소진 : {PayloadEncoder.GetString(context.messageBuffer, 0, maxReceiveLength)}");


                    if (currentReceived == 0)
                    {
                        context.isConnected = false;
                    }
                    else if (receivedMessageBufferLength == context.messageLength)
                    {
                        context.isOverflow = false;
                        break;
                    }

                    continue;
                }

                currentReceived = await stream.ReadAsync(context.messageBuffer, receivedMessageBufferLength, context.messageLength - receivedMessageBufferLength);

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료");
                    context.isConnected = false;
                    continue;
                }

                receivedMessageBufferLength += currentReceived;
                Log.Print($"receivedMsgBuffLength: {receivedMessageBufferLength}");

                if (receivedMessageBufferLength < context.messageLength)
                {
                    continue;
                }
                else if (receivedMessageBufferLength > context.messageLength)
                {
                    Log.Print("받기로 한 것보다 많은 데이터를 수신함");
                    context.isConnected = false;
                    continue;
                }

                context.fullMessage = PayloadEncoder.GetString(context.messageBuffer, 0, context.messageLength);

                Log.Print($"{context.fullMessage} at {DateTime.Now}");

                _ = EchoSendMessage(stream, context);

                break;
            }
        }

        private async Task EchoSendMessage(NetworkStream stream, ReceiveContext context)
        {
            byte[] fullBuffer = new byte[context.sizeBuffer.Length + context.messageLength];
            Buffer.BlockCopy(context.sizeBuffer, 0, fullBuffer, 0, context.sizeBuffer.Length);
            Buffer.BlockCopy(context.messageBuffer, 0, fullBuffer, context.sizeBuffer.Length, context.messageLength);

            Log.Print($"송신: 사이즈 ({PayloadEncoder.SizeBuffer2Num(context.sizeBuffer)}), 메시지 ({PayloadEncoder.GetString(context.messageBuffer, 0, context.messageBuffer.Length)})");

            // 비동기 송신
            await stream.WriteAsync(fullBuffer, 0, fullBuffer.Length);
        }


        private async Task Proccess(TcpClient tc)
        {
            NetworkStream stream = tc.GetStream();
            ReceiveContext context = new ReceiveContext();

            try
            {
                while (context.isConnected)
                {
                    await ReceiveSize(stream, context);
                    await ReceiveMessage(stream, context);
                }

                Release(tc, stream);
            }
            catch (Exception ex)
            {
                Log.Print($"연결 종료\n{ex}");
            }
        }
    }


    internal class Program
    {
        static Config config = new Config();

        static void Main(string[] args)
        {
#if !DEBUG
            string fileName = "config.json";
            string jsonString = File.ReadAllText(fileName);
            Config config = JsonSerializer.Deserialize<Config>(jsonString)!;
#else
            config.PrintLevel = LogLevel.DEBUG;
            config.Port = 7000;
#endif

            Log.PrintLevel = config.PrintLevel;
            Log.PrintHeader();

            Server server = new Server();
            server.Run().Wait();

            ///////////////////////////////

            //AysncEchoServer().Wait();
        }

        async static Task AysncEchoServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, config.Port);
            listener.Start();
            while (true)
            {
                // 비동기 Accept                
                TcpClient tc = await listener.AcceptTcpClientAsync();

                //tc.SendTimeout = 1000;
                //tc.ReceiveTimeout = 1000;

                // 새 쓰레드에서 처리
                _ = Task.Run(() =>
                {
                    AsyncTcpEcho(tc);
                });
            }
        }

        async static void AsyncTcpEcho(TcpClient tc)
        {
            NetworkStream stream = tc.GetStream();

            // 비동기 수신
            ushort msgBufferLength = 0xFFFF;
            var msgBuff = new byte[msgBufferLength];
            string msg = "";
            int receivedMsgBuffLength = 0;

            int sizeBuffLength = 2;
            byte[] sizeBuff = new byte[sizeBuffLength];
            int msgSize = 0;
            int sizeReceivedBuffLength = 0;

            bool isConnected = true;
            bool isOverflow = false;
            int currentReceived = -1;

            try
            {
                while (isConnected)
                {
                    while (isConnected)
                    {
                        currentReceived = await stream.ReadAsync(sizeBuff, sizeReceivedBuffLength, sizeBuffLength - sizeReceivedBuffLength);
                        sizeReceivedBuffLength += currentReceived;
                        Log.Print($"sizeReceivedBuffLength: {sizeReceivedBuffLength}");

                        if (currentReceived == 0)
                        {
                            Log.Print("0 byte 수신하여 종료");
                            isConnected = false;
                            continue;
                        }
                        else if (sizeReceivedBuffLength < sizeBuffLength)
                        {
                            continue;
                        }
                        else if (sizeReceivedBuffLength > sizeBuffLength)
                        {
                            Log.Print("메세지 사이즈 버퍼 크기를 초과하여 수신함");
                            isConnected = false;
                            continue;
                        }

                        msgSize = BitConverter.ToInt16(sizeBuff);

                        if (msgSize > msgBufferLength)
                        {
                            Log.Print($"메시지 버퍼 크기를 초과하여 수신할 수 없음 : msgSize({msgSize}) > msgBufferLength({msgBufferLength})");
                            isOverflow = true;
                        }

                        break;
                    }

                    sizeReceivedBuffLength = 0;

                    while (isConnected)
                    {
                        if (isOverflow) // 메세지 버퍼 크기 초과하여 수신하려 한 케이스의 입력을 모두 제거
                        {
                            currentReceived = await stream.ReadAsync(msgBuff, 0, Math.Min(msgBufferLength, msgSize - receivedMsgBuffLength));
                            receivedMsgBuffLength += currentReceived;

                            if (currentReceived == 0)
                            {
                                isConnected = false;
                            }
                            else if (receivedMsgBuffLength == msgSize)
                            {
                                isOverflow = false;
                                break;
                            }

                            continue;
                        }

                        currentReceived = await stream.ReadAsync(msgBuff, receivedMsgBuffLength, msgSize - receivedMsgBuffLength);

                        if (currentReceived == 0)
                        {
                            Log.Print("0 byte 수신하여 종료");
                            isConnected = false;
                            continue;
                        }

                        receivedMsgBuffLength += currentReceived;
                        Log.Print($"receivedMsgBuffLength: {receivedMsgBuffLength}");

                        if (receivedMsgBuffLength < msgSize)
                        {
                            continue;
                        }
                        else if (receivedMsgBuffLength > msgSize)
                        {
                            Log.Print("받기로 한 것보다 많은 데이터를 수신함");
                            isConnected = false;
                            continue;
                        }

                        msg = PayloadEncoder.GetString(msgBuff, 0, msgSize);

                        Log.Print($"{msg} at {DateTime.Now}");

                        byte[] fullBuff = new byte[sizeBuff.Length + msgSize];
                        Buffer.BlockCopy(sizeBuff, 0, fullBuff, 0, sizeBuff.Length);
                        Buffer.BlockCopy(msgBuff, 0, fullBuff, sizeBuff.Length, msgSize);

                        // 비동기 송신
                        await stream.WriteAsync(fullBuff, 0, fullBuff.Length);

                        break;
                    }

                    receivedMsgBuffLength = 0;
                }

                stream.Close();
                tc.Close();
            }
            catch (Exception ex)
            {
                Log.Print($"연결 종료\n{ex}");
            }
        }

    }
}

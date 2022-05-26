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
        public int expectedMessageLength { get; set; }
        public byte[] sizeBuffer { get; set; }
        public byte[] messageBuffer { get; set; }
        public string fullMessage { get; set; }

        public ReceiveContext()
        {
            isConnected = true;
            isOverflow = false;
            expectedMessageLength = 0;
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
            if (!context.isConnected)
            {
                Log.Print("연결이 끊겨있어 메시지 크기를 수신할 수 없음", LogLevel.WARN);
                return;
            }

            int receivedSizeBufferLength = 0;

            while (context.isConnected)
            {
                int currentReceived;

                currentReceived = await stream.ReadAsync(context.sizeBuffer, receivedSizeBufferLength, PayloadEncoder.MAX_SIZE_BUFFER_LENGTH - receivedSizeBufferLength);
                receivedSizeBufferLength += currentReceived;
                Log.Print($"sizeReceivedBuffLength: {receivedSizeBufferLength}", LogLevel.INFO);

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedSizeBufferLength > PayloadEncoder.MAX_SIZE_BUFFER_LENGTH)
                {
                    Log.Print("받기로 한 것보다 큰 메시지 크기 바이트를 수신함", LogLevel.WARN);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedSizeBufferLength < PayloadEncoder.MAX_SIZE_BUFFER_LENGTH)
                {
                    continue;
                }

                context.expectedMessageLength = PayloadEncoder.SizeBuffer2Num(context.sizeBuffer);

                break;
            }
        }

        private async Task RemoveOverflow(NetworkStream stream, ReceiveContext context)
        {
            if (!context.isConnected)
            {
                Log.Print("연결이 끊겨있어 오버플로된 수신 메시지를 소진할 수 없음", LogLevel.WARN);
                return;
            }

            int receivedMessageBufferLength = 0;
            int currentReceived;

            while (context.isOverflow && context.isConnected)
            {
                int maxReceiveLength = Math.Min(PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH, context.expectedMessageLength - receivedMessageBufferLength);
                currentReceived = await stream.ReadAsync(context.messageBuffer, 0, maxReceiveLength);
                receivedMessageBufferLength += currentReceived;
                Log.Print($"오버플로된 수신 메시지 : {PayloadEncoder.GetString(context.messageBuffer, 0, maxReceiveLength)}", LogLevel.WARN);

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    context.isConnected = false;
                }
                else if (receivedMessageBufferLength == context.expectedMessageLength)
                {
                    Log.Print("오버플로된 수신 메시지를 모두 소진함", LogLevel.INFO);
                    context.isOverflow = false;
                }
            }
        }

        private async Task ReceiveExpect(NetworkStream stream, ReceiveContext context)
        {
            if (!context.isConnected)
            {
                Log.Print("연결이 끊겨있어 메시지를 수신할 수 없음", LogLevel.WARN);
                return;
            }

            int receivedMessageBufferLength = 0;

            while (context.isConnected)
            {
                int currentReceived = await stream.ReadAsync(context.messageBuffer, receivedMessageBufferLength, context.expectedMessageLength - receivedMessageBufferLength);
                receivedMessageBufferLength += currentReceived;
                Log.Print($"receivedMsgBuffLength: {receivedMessageBufferLength}", LogLevel.INFO);

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedMessageBufferLength > context.expectedMessageLength)
                {
                    Log.Print("받기로 한 것보다 큰 메시지 바이트를 수신함", LogLevel.WARN);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedMessageBufferLength < context.expectedMessageLength)
                {
                    continue;
                }

                context.fullMessage = PayloadEncoder.GetString(context.messageBuffer, 0, context.expectedMessageLength);

                Log.Print($"{context.fullMessage} at {DateTime.Now}", LogLevel.INFO);

                break;
            }
        }

        private async Task SendEchoMessage(NetworkStream stream, ReceiveContext context)
        {
            if (!context.isConnected)
            {
                Log.Print("연결이 끊겨있어 메시지를 송신할 수 없음", LogLevel.WARN);
                return;
            }

            byte[] fullBuffer = new byte[context.sizeBuffer.Length + context.expectedMessageLength];
            Buffer.BlockCopy(context.sizeBuffer, 0, fullBuffer, 0, context.sizeBuffer.Length);
            Buffer.BlockCopy(context.messageBuffer, 0, fullBuffer, context.sizeBuffer.Length, context.expectedMessageLength);

            Log.Print($"송신: 사이즈 ({PayloadEncoder.SizeBuffer2Num(context.sizeBuffer)}), 메시지 ({PayloadEncoder.GetString(context.messageBuffer, 0, context.expectedMessageLength)})", LogLevel.INFO);

            // 비동기 송신
            await stream.WriteAsync(fullBuffer, 0, fullBuffer.Length);
        }

        private async Task SendMessage(NetworkStream stream, ReceiveContext context, string message)
        {
            if (!context.isConnected)
            {
                Log.Print("연결이 끊겨있어 메시지를 송신할 수 없음", LogLevel.WARN);
                return;
            }

            byte[] messageBuffer = PayloadEncoder.GetBytes(message);
            int messageBufferLength = messageBuffer.Length;

            byte[] sizeBuffer = PayloadEncoder.Num2SizeBuffer(messageBufferLength);
            byte[] fullBuffer = new byte[PayloadEncoder.MAX_SIZE_BUFFER_LENGTH + messageBufferLength];

            Buffer.BlockCopy(sizeBuffer, 0, fullBuffer, 0, sizeBuffer.Length);
            Buffer.BlockCopy(messageBuffer, 0, fullBuffer, sizeBuffer.Length, messageBuffer.Length);

            Log.Print($"송신: 사이즈 ({PayloadEncoder.SizeBuffer2Num(sizeBuffer)}), 메시지 ({PayloadEncoder.GetString(messageBuffer, 0, messageBuffer.Length)})", LogLevel.INFO);

            // 비동기 송신
            await stream.WriteAsync(fullBuffer, 0, fullBuffer.Length);
        }

        private async Task ReceiveMessage(NetworkStream stream, ReceiveContext context)
        {
            await ReceiveSize(stream, context);

            if (context.expectedMessageLength > PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH)
            {
                Log.Print($"메시지 버퍼 크기를 초과하여 수신할 수 없음 : expectedMessageLength({context.expectedMessageLength}) > MESSAGE_BUFFER_LENGTH({PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH})", LogLevel.WARN);
                context.isOverflow = true;
                await RemoveOverflow(stream, context);
                return;
            }

            await ReceiveExpect(stream, context);
            await SendEchoMessage(stream, context);
        }


        private async Task Proccess(TcpClient tc)
        {
            NetworkStream stream = tc.GetStream();
            ReceiveContext context = new ReceiveContext();

            try
            {
                while (context.isConnected)
                {
                    await ReceiveMessage(stream, context);
                }

                Release(tc, stream);
            }
            catch (Exception ex)
            {
                Log.Print($"연결 종료\n{ex}", LogLevel.INFO);
            }
        }
    }


    internal class Program
    {
        static Config config = new Config();

        static async Task Main(string[] args)
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
            await server.Run();
        }
    }
}

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
        public const int MAX_SIZE_BYTES_LENGTH = 1;
        /// <summary>
        /// 메시지 버퍼 최대 길이. 2^(8 * MAX_SIZE_BYTES_LENGTH)-1
        /// </summary>
        public static int MAX_MESSAGE_BYTES_LENGTH = 1 << (8 * MAX_SIZE_BYTES_LENGTH) - 1;

        public static int Bytes2Num(byte[] SizeBytes)
        {
            int ret = 0;
            for (int i = 0; i < SizeBytes.Length; i++)
            {
                ret <<= 1;
                ret += SizeBytes[i];
            }
            return ret;
        }

        public static byte[] Num2SizeBytes(int num)
        {
            return BitConverter.GetBytes(num)[..MAX_SIZE_BYTES_LENGTH];
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
        public int expectedMessageBytesLength { get; set; }
        public byte[] sizeBytes { get; set; }
        public byte[] messageBytes { get; set; }
        public string fullMessageBytes { get; set; }

        public ReceiveContext()
        {
            isConnected = true;
            isOverflow = false;
            expectedMessageBytesLength = 0;
            sizeBytes = new byte[PayloadEncoder.MAX_SIZE_BYTES_LENGTH];
            messageBytes = new byte[PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH];
            fullMessageBytes = "";
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

            int receivedSizeBytesLength = 0;

            while (context.isConnected)
            {
                int currentReceived;

                currentReceived = await stream.ReadAsync(context.sizeBytes, receivedSizeBytesLength, PayloadEncoder.MAX_SIZE_BYTES_LENGTH - receivedSizeBytesLength);
                receivedSizeBytesLength += currentReceived;
                Log.Print($"sizeReceivedBuffLength: {receivedSizeBytesLength}", LogLevel.INFO);

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedSizeBytesLength > PayloadEncoder.MAX_SIZE_BYTES_LENGTH)
                {
                    Log.Print("받기로 한 것보다 큰 메시지 크기 바이트를 수신함", LogLevel.WARN);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedSizeBytesLength < PayloadEncoder.MAX_SIZE_BYTES_LENGTH)
                {
                    continue;
                }

                context.expectedMessageBytesLength = PayloadEncoder.Bytes2Num(context.sizeBytes);

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

            int receivedMessageBytesLength = 0;
            int currentReceived;

            while (context.isOverflow && context.isConnected)
            {
                int maxReceiveLength = Math.Min(PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH, context.expectedMessageBytesLength - receivedMessageBytesLength);
                currentReceived = await stream.ReadAsync(context.messageBytes, 0, maxReceiveLength);
                receivedMessageBytesLength += currentReceived;
                Log.Print($"오버플로된 수신 메시지 : {PayloadEncoder.GetString(context.messageBytes, 0, maxReceiveLength)}", LogLevel.WARN);

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    context.isConnected = false;
                }
                else if (receivedMessageBytesLength == context.expectedMessageBytesLength)
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

            int receivedMessageBytesLength = 0;

            while (context.isConnected)
            {
                int currentReceived = await stream.ReadAsync(context.messageBytes, receivedMessageBytesLength, context.expectedMessageBytesLength - receivedMessageBytesLength);
                receivedMessageBytesLength += currentReceived;
                Log.Print($"receivedMsgBuffLength: {receivedMessageBytesLength}", LogLevel.INFO);

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedMessageBytesLength > context.expectedMessageBytesLength)
                {
                    Log.Print("받기로 한 것보다 큰 메시지 바이트를 수신함", LogLevel.WARN);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedMessageBytesLength < context.expectedMessageBytesLength)
                {
                    continue;
                }

                context.fullMessageBytes = PayloadEncoder.GetString(context.messageBytes, 0, context.expectedMessageBytesLength);

                Log.Print($"{context.fullMessageBytes} at {DateTime.Now}", LogLevel.INFO);

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

            byte[] fullBytes = new byte[context.sizeBytes.Length + context.expectedMessageBytesLength];
            Buffer.BlockCopy(context.sizeBytes, 0, fullBytes, 0, context.sizeBytes.Length);
            Buffer.BlockCopy(context.messageBytes, 0, fullBytes, context.sizeBytes.Length, context.expectedMessageBytesLength);

            Log.Print($"송신: 사이즈 ({PayloadEncoder.Bytes2Num(context.sizeBytes)}), 메시지 ({PayloadEncoder.GetString(context.messageBytes, 0, context.expectedMessageBytesLength)})", LogLevel.INFO);

            // 비동기 송신
            await stream.WriteAsync(fullBytes, 0, fullBytes.Length);
        }

        private async Task SendMessage(NetworkStream stream, ReceiveContext context, string message)
        {
            if (!context.isConnected)
            {
                Log.Print("연결이 끊겨있어 메시지를 송신할 수 없음", LogLevel.WARN);
                return;
            }

            byte[] messageBytes = PayloadEncoder.GetBytes(message);
            int messageBytesLength = messageBytes.Length;

            byte[] sizeBytes = PayloadEncoder.Num2SizeBytes(messageBytesLength);
            byte[] fullBytes = new byte[PayloadEncoder.MAX_SIZE_BYTES_LENGTH + messageBytesLength];

            Buffer.BlockCopy(sizeBytes, 0, fullBytes, 0, sizeBytes.Length);
            Buffer.BlockCopy(messageBytes, 0, fullBytes, sizeBytes.Length, messageBytes.Length);

            Log.Print($"송신: 사이즈 ({PayloadEncoder.Bytes2Num(sizeBytes)}), 메시지 ({PayloadEncoder.GetString(messageBytes, 0, messageBytes.Length)})", LogLevel.INFO);

            // 비동기 송신
            await stream.WriteAsync(fullBytes, 0, fullBytes.Length);
        }

        private async Task ReceiveMessage(NetworkStream stream, ReceiveContext context)
        {
            await ReceiveSize(stream, context);

            if (context.expectedMessageBytesLength > PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH)
            {
                Log.Print($"메시지 버퍼 크기를 초과하여 수신할 수 없음 : expectedMessageLength({context.expectedMessageBytesLength}) > MESSAGE_BYTES_LENGTH({PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH})", LogLevel.WARN);
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

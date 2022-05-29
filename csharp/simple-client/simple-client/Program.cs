using System.Net.Sockets;
using System.Net;
using System;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Collections.Generic;


namespace simple_client
{
    public class Object2FileHelper<T>
    {
        private string FilePath;
        private JsonSerializerOptions Options = new() { WriteIndented = true, IncludeFields = true, };

        public Object2FileHelper(string filePath)
        {
            FilePath = filePath;
        }

        public async Task Save(T obj)
        {
            using FileStream createStream = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(createStream, obj, Options);
            await createStream.DisposeAsync();
        }

        public async Task<T> Load()
        {
            using FileStream openStream = File.OpenRead(FilePath);
            T? obj = await JsonSerializer.DeserializeAsync<T>(openStream);
            return obj;
        }
    }

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

    class ReceiveContext
    {
        public int expectedMessageBytesLength { get; set; }
        public byte[] sizeBytes { get; set; }
        public byte[] messageBytes { get; set; }
        public string messageStr { get; set; }

        public ReceiveContext()
        {
            expectedMessageBytesLength = 0;
            sizeBytes = new byte[PayloadEncoder.MAX_SIZE_BYTES_LENGTH];
            messageBytes = new byte[PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH];
            messageStr = "";
        }

        private string GetBytes2HexStr(byte[] bytes, int len)
        {
            if (bytes.Length < len)
                return $"GetBytes2HexStr: 주어진 길이가 실제 바이트 배열의 길이를 초과함. {bytes.Length} < {len}";

            StringBuilder sb = new StringBuilder();
            sb.Append(BitConverter.ToString(bytes[..len]));

            return sb.ToString();
        }

        public override string ToString()
        {
            return $"{nameof(expectedMessageBytesLength)}: {expectedMessageBytesLength}\n{nameof(sizeBytes)}: {GetBytes2HexStr(sizeBytes, PayloadEncoder.MAX_SIZE_BYTES_LENGTH)}\n{nameof(messageBytes)}: {GetBytes2HexStr(messageBytes, expectedMessageBytesLength)}\n{nameof(messageStr)}: {messageStr}";
        }
    }

    class ConnectionContext
    {
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public string Cid { get; set; }
        public bool isConnected { get; set; }
        public ReceiveContext ReceiveContext { get; set; }

        public ConnectionContext(TcpClient client, NetworkStream stream, string cid)
        {
            Client = client;
            Stream = stream;
            Cid = cid;
            isConnected = true;
            ReceiveContext = new ReceiveContext();
        }

        public void Release()
        {
            Stream.Close();
            Client.Close();
            isConnected = false;
        }

        public override string ToString()
        {
            return $"{nameof(Cid)}: {Cid}\n{nameof(isConnected)}: {isConnected}\n{nameof(ReceiveContext)}: {ReceiveContext}";
        }
    }


    public class ReceiveOverflowException : Exception
    {
        public ReceiveOverflowException()
        {
        }

        public ReceiveOverflowException(string message)
            : base(message)
        {
        }

        public ReceiveOverflowException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    static class TcpClientUtility
    {

        public static async Task ReceiveSize(ConnectionContext context)
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

                currentReceived = await context.Stream.ReadAsync(context.ReceiveContext.sizeBytes, receivedSizeBytesLength, PayloadEncoder.MAX_SIZE_BYTES_LENGTH - receivedSizeBytesLength);
                receivedSizeBytesLength += currentReceived;

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

                context.ReceiveContext.expectedMessageBytesLength = PayloadEncoder.Bytes2Num(context.ReceiveContext.sizeBytes);

                break;
            }
        }

        public static async Task RemoveOverflow(ConnectionContext context)
        {
            if (!context.isConnected)
            {
                Log.Print("연결이 끊겨있어 오버플로된 수신 메시지를 소진할 수 없음", LogLevel.WARN);
                return;
            }

            int receivedMessageBytesLength = 0;
            int currentReceived;

            while (context.isConnected)
            {
                int maxReceiveLength = Math.Min(PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH, context.ReceiveContext.expectedMessageBytesLength - receivedMessageBytesLength);
                currentReceived = await context.Stream.ReadAsync(context.ReceiveContext.messageBytes, 0, maxReceiveLength);
                receivedMessageBytesLength += currentReceived;
                Log.Print($"오버플로된 수신 메시지 : {PayloadEncoder.GetString(context.ReceiveContext.messageBytes, 0, maxReceiveLength)}", LogLevel.WARN);

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    context.isConnected = false;
                }
                else if (receivedMessageBytesLength == context.ReceiveContext.expectedMessageBytesLength)
                {
                    Log.Print("오버플로된 수신 메시지를 모두 소진함", LogLevel.INFO);
                    context.ReceiveContext.expectedMessageBytesLength = 0;
                    return;
                }
            }
        }

        public static async Task ReceiveExpect(ConnectionContext context)
        {
            if (!context.isConnected)
            {
                Log.Print("연결이 끊겨있어 메시지를 수신할 수 없음", LogLevel.WARN);
                return;
            }

            int receivedMessageBytesLength = 0;

            while (context.isConnected)
            {
                int currentReceived = await context.Stream.ReadAsync(context.ReceiveContext.messageBytes, receivedMessageBytesLength, context.ReceiveContext.expectedMessageBytesLength - receivedMessageBytesLength);
                receivedMessageBytesLength += currentReceived;

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedMessageBytesLength > context.ReceiveContext.expectedMessageBytesLength)
                {
                    Log.Print("받기로 한 것보다 큰 메시지 바이트를 수신함", LogLevel.WARN);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedMessageBytesLength < context.ReceiveContext.expectedMessageBytesLength)
                {
                    continue;
                }

                context.ReceiveContext.messageStr = PayloadEncoder.GetString(context.ReceiveContext.messageBytes, 0, context.ReceiveContext.expectedMessageBytesLength);

                Log.Print($"수신: 사이즈 ({PayloadEncoder.Bytes2Num(context.ReceiveContext.sizeBytes)}), 메시지 ({PayloadEncoder.GetString(context.ReceiveContext.messageBytes, 0, context.ReceiveContext.expectedMessageBytesLength)})", LogLevel.INFO);

                break;
            }
        }

        public static async Task SendLastReceivedMessage(ConnectionContext src, ConnectionContext dst)
        {
            if (!dst.isConnected)
            {
                Log.Print("연결이 끊겨있어 메시지를 송신할 수 없음", LogLevel.WARN);
                return;
            }

            byte[] fullBytes = new byte[src.ReceiveContext.sizeBytes.Length + src.ReceiveContext.expectedMessageBytesLength];
            Buffer.BlockCopy(src.ReceiveContext.sizeBytes, 0, fullBytes, 0, src.ReceiveContext.sizeBytes.Length);
            Buffer.BlockCopy(src.ReceiveContext.messageBytes, 0, fullBytes, src.ReceiveContext.sizeBytes.Length, src.ReceiveContext.expectedMessageBytesLength);

            Log.Print($"송신: 사이즈 ({PayloadEncoder.Bytes2Num(src.ReceiveContext.sizeBytes)}), 메시지 ({PayloadEncoder.GetString(src.ReceiveContext.messageBytes, 0, src.ReceiveContext.expectedMessageBytesLength)})", LogLevel.INFO);

            // 비동기 송신
            await dst.Stream.WriteAsync(fullBytes, 0, fullBytes.Length);
        }

        public static async Task SendMessage(ConnectionContext dst, string message)
        {
            if (!dst.isConnected)
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
            await dst.Stream.WriteAsync(fullBytes, 0, fullBytes.Length);
        }

        public static async Task ReceiveMessage(ConnectionContext context)
        {
            await ReceiveSize(context);

            if (context.ReceiveContext.expectedMessageBytesLength > PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH)
            {
                Log.Print($"메시지 버퍼 크기를 초과하여 수신할 수 없음 : expectedMessageLength({context.ReceiveContext.expectedMessageBytesLength}) > MESSAGE_BYTES_LENGTH({PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH})", LogLevel.WARN);
                await RemoveOverflow(context);
                throw new ReceiveOverflowException();
            }

            await ReceiveExpect(context);
        }

        public static string GetMessage()
        {
            string msg = Console.ReadLine();
            byte[] messageBytes = PayloadEncoder.GetBytes(msg);

            if (messageBytes.Length > PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH)
            {
                Log.Print($"메시지가 너무 깁니다. {PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH} 이하로 입력하세요.", LogLevel.WARN);
                return null;
            }

            return msg;
        }
    }

    class Client
    {
        private string ServerAddress;
        private int Port;

        public Client(string serverAddress, int port)
        {
            ServerAddress = serverAddress;
            Port = port;
        }

        public async Task Run()
        {
            TcpClient tc = new TcpClient(ServerAddress, Port);
            NetworkStream stream = tc.GetStream();
            ConnectionContext context = new ConnectionContext(tc, stream, $"{tc.Client.RemoteEndPoint}-{tc.Client.LocalEndPoint}");

            await Proccess(context, TcpClientUtility.GetMessage, 1000);

            context.Release();
            Log.Print($"연결 종료", LogLevel.INFO);
        }

        public async Task TestRun(int sendDelay)
        {
            TcpClient tc = new TcpClient(ServerAddress, Port);
            NetworkStream stream = tc.GetStream();
            ConnectionContext context = new ConnectionContext(tc, stream, $"{tc.Client.RemoteEndPoint}-{tc.Client.LocalEndPoint}");

            Log.Print($"{context.Cid} 연결 수립", LogLevel.OFF);

            Func<string> getMessage = () =>
            {
                return context.Cid;
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
            }
            Log.Print($"{context.Cid} connection 리소스 해제 완료", LogLevel.INFO);
        }

        private async Task Proccess(ConnectionContext context, Func<string> getMessage, int sendDelay)
        {
            _ = Task.Run(async () =>
            {
                while (context.isConnected)
                {
                    Log.Print(context.ToString(), LogLevel.DEBUG);
                    try
                    {
                        await TcpClientUtility.ReceiveMessage(context);
                        Log.Print($"received message: {context.ReceiveContext.messageStr}");
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
                if (message == null)
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

    class Config
    {
        [JsonInclude]
        public LogLevel PrintLevel { get; set; }
        public string ServerAddress { get; set; }
        [JsonInclude]
        public int Port { get; set; }

        public Config()
        {
            //ServerAddress = "localhost";
            ServerAddress = "192.168.0.53";
            PrintLevel = LogLevel.DEBUG;
            Port = 7000;
        }
    }


    internal class Program
    {
        static Config Config = new Config();
        static Object2FileHelper<Config> Config2FileHelper;

        static async Task Main(string[] args)
        {
#if DEBUG
#else
            string fileName = "config.json";
            Config2FileHelper = new Object2FileHelper<Config>(fileName);
            try
            {
                Config = await Config2FileHelper.Load();
            }
            catch (FileNotFoundException ex)
            {
                await Config2FileHelper.Save(Config);
            }
#endif
            Log.PrintLevel = Config.PrintLevel;
            Log.PrintHeader();

            /////////////////////////////////////////////////////////

            //Client client = new Client(Config.ServerAddress, Config.Port);
            //await client.Run();

            /////////////////////////////////////////////////////////

            int runningClientNum = 500;
            int connectionDelay = 100;
            int sendDelay = 1000;
            Log.PrintLevel = LogLevel.ERROR;

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < runningClientNum; i++)
            {
                Client client = new Client(Config.ServerAddress, Config.Port);
                await Task.Delay(connectionDelay);
                tasks.Add(client.TestRun(sendDelay));
            }


            await Task.WhenAll(tasks);

            Log.Print("전체 종료", LogLevel.OFF);
        }
    }
}

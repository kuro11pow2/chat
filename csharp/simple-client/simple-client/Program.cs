using System.Net.Sockets;
using System.Net;
using System;
using System.Threading;
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

    public class PayloadEncoderOverflowException : Exception
    {
        public PayloadEncoderOverflowException()
        {
        }

        public PayloadEncoderOverflowException(string message)
            : base(message)
        {
        }

        public PayloadEncoderOverflowException(string message, Exception inner)
            : base(message, inner)
        {
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
            byte[] tmp = Encoding.UTF8.GetBytes(str);
            if (tmp.Length > MAX_MESSAGE_BYTES_LENGTH)
                throw new PayloadEncoderOverflowException($"{MAX_MESSAGE_BYTES_LENGTH} bytes 초과");
            return tmp;
        }

    }

    class ReceiveContext
    {
        public int expectedMessageBytesLength { get; set; }
        public byte[] sizeBytes { get; set; }
        public byte[] messageBytes { get; set; }
        public byte[] fullBytes { get; set; }
        public string messageStr { get; set; }

        public ReceiveContext()
        {
            expectedMessageBytesLength = 0;
            sizeBytes = new byte[PayloadEncoder.MAX_SIZE_BYTES_LENGTH];
            messageBytes = new byte[PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH];
            fullBytes = new byte[PayloadEncoder.MAX_SIZE_BYTES_LENGTH + PayloadEncoder.MAX_MESSAGE_BYTES_LENGTH];
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
            return $"{nameof(expectedMessageBytesLength)}: {expectedMessageBytesLength}\n{nameof(sizeBytes)}: {GetBytes2HexStr(sizeBytes, PayloadEncoder.MAX_SIZE_BYTES_LENGTH)}\n{nameof(messageBytes)}: {GetBytes2HexStr(messageBytes, expectedMessageBytesLength)}\n{nameof(fullBytes)}: {GetBytes2HexStr(fullBytes, PayloadEncoder.MAX_SIZE_BYTES_LENGTH + expectedMessageBytesLength)}\n{nameof(messageStr)}: {messageStr}";
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

                currentReceived = await context.Stream.ReadAsync(context.ReceiveContext.sizeBytes, receivedSizeBytesLength, context.ReceiveContext.sizeBytes.Length - receivedSizeBytesLength);
                receivedSizeBytesLength += currentReceived;

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedSizeBytesLength > context.ReceiveContext.sizeBytes.Length)
                {
                    Log.Print("받기로 한 것보다 큰 메시지 크기 바이트를 수신함", LogLevel.WARN);
                    context.isConnected = false;
                    continue;
                }
                else if (receivedSizeBytesLength < context.ReceiveContext.sizeBytes.Length)
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
                int maxReceiveLength = Math.Min(context.ReceiveContext.messageBytes.Length, context.ReceiveContext.expectedMessageBytesLength - receivedMessageBytesLength);
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
                Buffer.BlockCopy(context.ReceiveContext.sizeBytes, 0, context.ReceiveContext.fullBytes, 0, context.ReceiveContext.sizeBytes.Length);
                Buffer.BlockCopy(context.ReceiveContext.messageBytes, 0, context.ReceiveContext.fullBytes, context.ReceiveContext.sizeBytes.Length, context.ReceiveContext.expectedMessageBytesLength);

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

            Log.Print($"송신: 사이즈 ({src.ReceiveContext.expectedMessageBytesLength}), 메시지 ({PayloadEncoder.GetString(src.ReceiveContext.messageBytes, 0, src.ReceiveContext.expectedMessageBytesLength)})", LogLevel.INFO);

            // 비동기 송신
            await dst.Stream.WriteAsync(src.ReceiveContext.fullBytes, 0, src.ReceiveContext.sizeBytes.Length + src.ReceiveContext.expectedMessageBytesLength);
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
            byte[] fullBytes = new byte[sizeBytes.Length + messageBytesLength];

            Buffer.BlockCopy(sizeBytes, 0, fullBytes, 0, sizeBytes.Length);
            Buffer.BlockCopy(messageBytes, 0, fullBytes, sizeBytes.Length, messageBytes.Length);

            Log.Print($"송신: 사이즈 ({PayloadEncoder.Bytes2Num(sizeBytes)}), 메시지 ({PayloadEncoder.GetString(messageBytes, 0, messageBytes.Length)})", LogLevel.INFO);

            // 비동기 송신
            await dst.Stream.WriteAsync(fullBytes, 0, fullBytes.Length);
        }

        public static async Task ReceiveMessage(ConnectionContext context)
        {
            await ReceiveSize(context);

            if (context.ReceiveContext.expectedMessageBytesLength > context.ReceiveContext.messageBytes.Length)
            {
                Log.Print($"메시지 버퍼 크기를 초과하여 수신할 수 없음 : expectedMessageLength({context.ReceiveContext.expectedMessageBytesLength}) > MESSAGE_BYTES_LENGTH({context.ReceiveContext.messageBytes.Length})", LogLevel.WARN);
                await RemoveOverflow(context);
                throw new ReceiveOverflowException();
            }

            await ReceiveExpect(context);
        }

        public static string GetMessage()
        {
            string msg = Console.ReadLine();

            try
            {
                byte[] messageBytes = PayloadEncoder.GetBytes(msg);
            }
            catch (PayloadEncoderOverflowException ex)
            {
                Log.Print($"{ex}", LogLevel.WARN);
                return null;
            }

            return msg;
        }
    }

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
                await Proccess(context, TcpClientUtility.GetMessage, 1000);
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

    class ThreadPoolUtility
    {
        public static void SetThreadCount(int worker, int completionPort)
        {
            ThreadPool.SetMinThreads(worker, completionPort);
        }
        public static string GetThreadPoolInfo()
        {
            int max_worker, min_worker, avail_worker;
            int max_completion_port, min_completion_port, avail_completion_port;
            ThreadPool.GetMaxThreads(out max_worker, out max_completion_port);
            ThreadPool.GetMinThreads(out min_worker, out min_completion_port);
            ThreadPool.GetAvailableThreads(out avail_worker, out avail_completion_port);
            return $"[{nameof(GetThreadPoolInfo)}]\n{nameof(max_worker)}: {max_worker}\n{nameof(min_worker)}: {min_worker}\n{nameof(avail_worker)}: {avail_worker}\n{nameof(max_completion_port)}: {max_completion_port}\n{nameof(min_completion_port)}: {min_completion_port}\n{nameof(avail_completion_port)}: {avail_completion_port}";
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
            PrintLevel = LogLevel.DEBUG;
            ServerAddress = "localhost";
            Port = 7000;
        }

        public override string ToString()
        {
            return $"[Config]\n{nameof(PrintLevel)}: {PrintLevel}\n{nameof(ServerAddress)}: {ServerAddress}\n{nameof(Port)}: {Port}";
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


            /////////////////////////////////////////////////////////
            //Log.PrintHeader();
            //Log.Print($"\n{Config}", LogLevel.OFF);
            //Log.PrintLevel = Config.PrintLevel;

            //Client client = new Client(Config.ServerAddress, Config.Port);
            //await client.Run();

            /////////////////////////////////////////////////////////

            int runningClientNum = 100;
            int connectionDelay = 50;
            int sendDelay = 1000;

            //Config.ServerAddress = "192.168.0.53";
            Config.Port = 7000;
            Config.PrintLevel = LogLevel.WARN;


            Log.PrintHeader();
            Log.Print($"\n{Config}", LogLevel.OFF);
            Log.Print($"\n{ThreadPoolUtility.GetThreadPoolInfo()}", LogLevel.OFF);
            Log.PrintLevel = Config.PrintLevel;

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < runningClientNum; i++)
            {
                Client client = new Client(Config.ServerAddress, Config.Port, i);
                await Task.Delay(connectionDelay);
                tasks.Add(client.TestRun(sendDelay));
            }

            await Task.WhenAll(tasks);

            Log.Print("전체 종료", LogLevel.OFF);
        }
    }
}

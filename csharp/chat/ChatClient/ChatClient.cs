using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using Common;
using Common.Utility;
using Common.Interface;


namespace Chat
{
    public class ChatClient : IClient
    {
        private static IEncoder Encoder = new Utf8PayloadEncoder();
        private string ServerAddress;
        private int Port;
        private int LocalId;
        private int SendDelay;
        private ConnectionContext? ConnectionContext { get; set; }
        private ReceiveContext ReceiveContext { get; set; }
        public string ReceivedMessage => ReceiveContext.messageStr;


        public ChatClient(string serverAddress, int port, int localId = -1, int sendDelay = 0)
        {
            ServerAddress = serverAddress;
            Port = port;
            LocalId = localId;
            SendDelay = sendDelay;
            ReceiveContext = new ReceiveContext();
        }


        public async Task Run()
        {
            await Connect();

            if (ConnectionContext == null)
                throw new Exception("ConnectionContext == null");

            try
            {
                _ = Task.Run(async () =>
                {
                    while (ConnectionContext.IsConnected)
                    {
                        Log.Print($"\n{ConnectionContext}\n{ReceiveContext}", LogLevel.DEBUG);
                        try
                        {
                            await Receive();
                        }
                        catch (ProtocolBufferOverflowException ex)
                        {
                            Log.Print($"{ex}", LogLevel.ERROR);
                        }
                    }
                });

                while (ConnectionContext.IsConnected)
                {
                    if (SendDelay > 0)
                        await Task.Delay(SendDelay);

                    string? message = Console.ReadLine();

                    if (string.IsNullOrEmpty(message))
                        continue;

                    try
                    {
                        await Send(message);
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

            Log.Print($"{ConnectionContext.Cid} 연결 종료", LogLevel.INFO);

            try
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                Log.Print($"{ConnectionContext.Cid} 리소스 해제 실패\n{ex}", LogLevel.ERROR);
                return;
            }
            Log.Print($"{ConnectionContext.Cid} connection 리소스 해제 완료", LogLevel.INFO);
        }

        public async Task Connect()
        {
            TcpClient client = await Task.Run(() =>
            {
                return new TcpClient(ServerAddress, Port);
            });
            NetworkStream stream = client.GetStream();
            string cid = $"{client.Client.RemoteEndPoint}-{client.Client.LocalEndPoint}";
            ConnectionContext = new ConnectionContext(client, stream, cid);

            Log.Print($"{cid} 연결 수립", LogLevel.OFF);
        }

        public void Disconnect()
        {
            if (ConnectionContext == null)
                throw new Exception("ConnectionContext가 null임");

            ConnectionContext.Stream.Close();
            ConnectionContext.Client.Close();
        }

        public async Task Receive()
        {
            await ReceiveSize();

            if (ReceiveContext.expectedMessageBytesLength > ReceiveContext.messageBytes.Length)
            {
                Log.Print($"메시지 버퍼 크기를 초과하여 수신할 수 없음 : expectedMessageLength({ReceiveContext.expectedMessageBytesLength}) > MESSAGE_BYTES_LENGTH({ReceiveContext.messageBytes.Length})", LogLevel.WARN);
                await RemoveOverflow();
                throw new ProtocolBufferOverflowException();
            }

            await ReceiveExpect();
        }

        public async Task Send(string message)
        {
            if (ConnectionContext == null || !ConnectionContext.IsConnected)
            {
                Log.Print("연결이 끊겨있어 메시지를 송신할 수 없음", LogLevel.WARN);
                return;
            }

            byte[] messageBytes = Encoder.Encode(message);
            int messageBytesLength = messageBytes.Length;

            byte[] sizeBytes = PayloadProtocol.EncodeSizeBytes(messageBytesLength);
            byte[] fullBytes = new byte[sizeBytes.Length + messageBytesLength];

            Buffer.BlockCopy(sizeBytes, 0, fullBytes, 0, sizeBytes.Length);
            Buffer.BlockCopy(messageBytes, 0, fullBytes, sizeBytes.Length, messageBytes.Length);

            Log.Print($"송신: 사이즈 ({messageBytesLength}), 메시지 ({message})", LogLevel.INFO);

            // 비동기 송신
            await ConnectionContext.Stream.WriteAsync(fullBytes, 0, fullBytes.Length);
        }



        private async Task ReceiveSize()
        {
            if (ConnectionContext == null || !ConnectionContext.IsConnected)
            {
                Log.Print("연결이 끊겨있어 메시지 크기를 수신할 수 없음", LogLevel.WARN);
                return;
            }

            int receivedSizeBytesLength = 0;

            while (ConnectionContext.IsConnected)
            {
                int currentReceived;

                currentReceived = await ConnectionContext.Stream.ReadAsync(ReceiveContext.sizeBytes, receivedSizeBytesLength, ReceiveContext.sizeBytes.Length - receivedSizeBytesLength);
                receivedSizeBytesLength += currentReceived;

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    ConnectionContext.IsConnected = false;
                    continue;
                }
                else if (receivedSizeBytesLength > ReceiveContext.sizeBytes.Length)
                {
                    Log.Print("받기로 한 것보다 큰 메시지 크기 바이트를 수신함", LogLevel.WARN);
                    ConnectionContext.IsConnected = false;
                    continue;
                }
                else if (receivedSizeBytesLength < ReceiveContext.sizeBytes.Length)
                {
                    continue;
                }

                ReceiveContext.expectedMessageBytesLength = PayloadProtocol.DecodeSizeBytes(ReceiveContext.sizeBytes);
                break;
            }
        }

        private async Task RemoveOverflow()
        {
            if (ConnectionContext == null || !ConnectionContext.IsConnected)
            {
                Log.Print("연결이 끊겨있어 오버플로된 수신 메시지를 소진할 수 없음", LogLevel.WARN);
                return;
            }

            int receivedMessageBytesLength = 0;
            int currentReceived;

            while (ConnectionContext.IsConnected)
            {
                int maxReceiveLength = Math.Min(ReceiveContext.messageBytes.Length, ReceiveContext.expectedMessageBytesLength - receivedMessageBytesLength);
                currentReceived = await ConnectionContext.Stream.ReadAsync(ReceiveContext.messageBytes, 0, maxReceiveLength);
                receivedMessageBytesLength += currentReceived;
                Log.Print($"오버플로된 수신 메시지 : {Encoder.Decode(ReceiveContext.messageBytes, 0, maxReceiveLength)}", LogLevel.WARN);

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    ConnectionContext.IsConnected = false;
                }
                else if (receivedMessageBytesLength == ReceiveContext.expectedMessageBytesLength)
                {
                    Log.Print("오버플로된 수신 메시지를 모두 소진함", LogLevel.INFO);
                    ReceiveContext.expectedMessageBytesLength = 0;
                    return;
                }
            }
        }

        private async Task ReceiveExpect()
        {
            if (ConnectionContext == null || !ConnectionContext.IsConnected)
            {
                Log.Print("연결이 끊겨있어 메시지를 수신할 수 없음", LogLevel.WARN);
                return;
            }

            int receivedMessageBytesLength = 0;

            while (ConnectionContext.IsConnected)
            {
                int currentReceived = await ConnectionContext.Stream.ReadAsync(ReceiveContext.messageBytes, receivedMessageBytesLength, ReceiveContext.expectedMessageBytesLength - receivedMessageBytesLength);
                receivedMessageBytesLength += currentReceived;

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    ConnectionContext.IsConnected = false;
                    continue;
                }
                else if (receivedMessageBytesLength > ReceiveContext.expectedMessageBytesLength)
                {
                    Log.Print("받기로 한 것보다 큰 메시지 바이트를 수신함", LogLevel.WARN);
                    ConnectionContext.IsConnected = false;
                    continue;
                }
                else if (receivedMessageBytesLength < ReceiveContext.expectedMessageBytesLength)
                {
                    continue;
                }

                ReceiveContext.messageStr = Encoder.Decode(ReceiveContext.messageBytes, 0, ReceiveContext.expectedMessageBytesLength);
                Buffer.BlockCopy(ReceiveContext.sizeBytes, 0, ReceiveContext.fullBytes, 0, ReceiveContext.sizeBytes.Length);
                Buffer.BlockCopy(ReceiveContext.messageBytes, 0, ReceiveContext.fullBytes, ReceiveContext.sizeBytes.Length, ReceiveContext.expectedMessageBytesLength);

                Log.Print($"수신: 사이즈 ({PayloadProtocol.DecodeSizeBytes(ReceiveContext.sizeBytes)}), 메시지 ({Encoder.Decode(ReceiveContext.messageBytes, 0, ReceiveContext.expectedMessageBytesLength)})", LogLevel.INFO);

                break;
            }
        }
    }
}

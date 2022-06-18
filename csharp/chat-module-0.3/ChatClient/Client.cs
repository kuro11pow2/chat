using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using Common;
using Common.Utility;
using Common.Interface;
using System.Net;

namespace Chat
{
    public class Client : IClient
    {
        private string DestinationAddress;
        private int Port;
        private int LocalId;
        private int SendDelay;
        private ConnectionContext? ConnectionContext { get; set; }

        public bool IsConnected { get { return ConnectionContext?.IsConnected ?? false; } }

        public string Cid { get { return ConnectionContext?.Cid ?? "NA"; } }

        public string Info { get { return $"{nameof(LocalId)}: {LocalId}\n{nameof(Cid)}: {Cid}\n{nameof(ConnectionContext)}: {ConnectionContext}\n{nameof(SendDelay)}: {SendDelay}\n"; } }


        public Client(string destinationAddress, int port, int localId = -1, int sendDelay = 0, ConnectionContext? connectionContext = null)
        {
            DestinationAddress = destinationAddress;
            Port = port;
            LocalId = localId;
            SendDelay = sendDelay;
            ConnectionContext = connectionContext;
        }

        public Client(TcpClient client, IPEndPoint endpoint, int localId = -1, int sendDelay = 0) : this(
            endpoint.Address.ToString(), endpoint.Port, localId, sendDelay,
            new ConnectionContext(client, client.GetStream(), $"{client.Client.LocalEndPoint}-{client.Client.RemoteEndPoint}")
            )
        {
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
                        Log.Print($"\n{ConnectionContext}", LogLevel.DEBUG);
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

                    string? str = Console.ReadLine();

                    if (string.IsNullOrEmpty(str))
                        continue;

                    IMessage message = new Utf8Message();
                    message.SetString(str);

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
                return new TcpClient(DestinationAddress, Port);
            });
            NetworkStream stream = client.GetStream();
            string cid = $"{client.Client.LocalEndPoint}-{client.Client.RemoteEndPoint}";
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

        public async Task<IMessage> Receive()
        {
            byte[] fullBytes = new byte[Utf8PayloadProtocol.SIZE_BYTES_LENGTH + Utf8PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH];
            int expectedMessageBytesLength = await ReceiveSize(fullBytes);

            if (expectedMessageBytesLength > Utf8PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH)
            {
                Log.Print($"메시지 버퍼 크기를 초과하여 수신할 수 없음 : expectedMessageLength({expectedMessageBytesLength}) > MESSAGE_BYTES_LENGTH({Utf8PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH})", LogLevel.WARN);
                await RemoveOverflow(fullBytes, expectedMessageBytesLength);
                throw new ProtocolBufferOverflowException();
            }

            IMessage message = await ReceiveExpect(fullBytes, expectedMessageBytesLength);
            return message;
        }

        public async Task Send(IMessage message)
        {
            if (ConnectionContext == null || !ConnectionContext.IsConnected)
            {
                Log.Print("연결이 끊겨있어 메시지를 송신할 수 없음", LogLevel.WARN);
                return;
            }

            var fullBytes = message.GetFullBytes();

            Log.Print($"송신: {message.GetInfo()}", LogLevel.INFO);

            // 비동기 송신
            await ConnectionContext.Stream.WriteAsync(fullBytes);
        }



        private async Task<int> ReceiveSize(byte[] fullBytes)
        {
            if (ConnectionContext == null || !ConnectionContext.IsConnected)
            {
                Log.Print("연결이 끊겨있어 메시지 크기를 수신할 수 없음", LogLevel.WARN);
                return -1;
            }

            int receivedSizeBytesLength = 0;

            while (ConnectionContext.IsConnected)
            {
                int currentReceived;

                currentReceived = await ConnectionContext.Stream.ReadAsync(fullBytes, receivedSizeBytesLength, Utf8PayloadProtocol.SIZE_BYTES_LENGTH - receivedSizeBytesLength);
                receivedSizeBytesLength += currentReceived;

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    ConnectionContext.IsConnected = false;
                    continue;
                }
                else if (receivedSizeBytesLength > Utf8PayloadProtocol.SIZE_BYTES_LENGTH)
                {
                    Log.Print("받기로 한 것보다 큰 메시지 크기 바이트를 수신함", LogLevel.WARN);
                    ConnectionContext.IsConnected = false;
                    continue;
                }
                else if (receivedSizeBytesLength < Utf8PayloadProtocol.SIZE_BYTES_LENGTH)
                {
                    continue;
                }

                break;
            }

            return Utf8PayloadProtocol.DecodeSizeBytes(fullBytes, 0, Utf8PayloadProtocol.SIZE_BYTES_LENGTH);
        }

        private async Task RemoveOverflow(byte[] fullBytes, int expectedMessageBytesLength)
        {
            if (ConnectionContext == null || !ConnectionContext.IsConnected)
            {
                Log.Print("연결이 끊겨있어 오버플로된 수신 메시지를 소진할 수 없음", LogLevel.WARN);
                return;
            }

            int receivedMessageBytesLength = 0;
            int currentReceived;
            int offset = Utf8PayloadProtocol.SIZE_BYTES_LENGTH;

            while (ConnectionContext.IsConnected)
            {
                int maxReceiveLength = Math.Min(Utf8PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH, expectedMessageBytesLength - receivedMessageBytesLength);
                currentReceived = await ConnectionContext.Stream.ReadAsync(fullBytes, offset, maxReceiveLength);
                receivedMessageBytesLength += currentReceived;
                Log.Print($"오버플로된 수신 메시지 : {Utf8PayloadProtocol.DecodeMessage(fullBytes, offset, maxReceiveLength)}", LogLevel.WARN);

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    ConnectionContext.IsConnected = false;
                }
                else if (receivedMessageBytesLength == expectedMessageBytesLength)
                {
                    Log.Print("오버플로된 수신 메시지를 모두 소진함", LogLevel.INFO);
                    return;
                }
            }
        }

        private async Task<IMessage> ReceiveExpect(byte[] fullBytes, int expectedMessageBytesLength)
        {
            IMessage message = new Utf8Message();

            if (ConnectionContext == null || !ConnectionContext.IsConnected)
            {
                Log.Print("연결이 끊겨있어 메시지를 수신할 수 없음", LogLevel.WARN);
                return message;
            }

            int receivedMessageBytesLength = 0;
            int offset = Utf8PayloadProtocol.SIZE_BYTES_LENGTH;

            while (ConnectionContext.IsConnected)
            {
                int currentReceived = await ConnectionContext.Stream.ReadAsync(fullBytes, offset + receivedMessageBytesLength, expectedMessageBytesLength - receivedMessageBytesLength);
                receivedMessageBytesLength += currentReceived;

                if (currentReceived == 0)
                {
                    Log.Print("0 byte 수신하여 종료", LogLevel.INFO);
                    ConnectionContext.IsConnected = false;
                    continue;
                }
                else if (receivedMessageBytesLength > expectedMessageBytesLength)
                {
                    Log.Print("받기로 한 것보다 큰 메시지 바이트를 수신함", LogLevel.WARN);
                    ConnectionContext.IsConnected = false;
                    continue;
                }
                else if (receivedMessageBytesLength < expectedMessageBytesLength)
                {
                    continue;
                }

                message.SetBytes(fullBytes, offset + expectedMessageBytesLength);


                Log.Print($"수신: {message.GetInfo()})", LogLevel.INFO);

                break;
            }

            return message;
        }
    }
}

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

using Nito.AsyncEx;

namespace Chat
{
    public class User : IClient
    {
        private int SendDelay { get; set; }
        private IConnectionContext ConnectionContext { get; set; }
        private bool MustBeDisconnected { get; set; } = false;

        public bool IsReady { get { return ConnectionContext.IsReady; } }

        public string Cid { get { return ConnectionContext?.ConnectionId ?? "NA"; } }

        public string Info { get { return $"{nameof(Cid)}: {Cid}\n{nameof(SendDelay)}: {SendDelay}\n{ConnectionContext}"; } }

        // https://github.com/StephenCleary/AsyncEx
        private readonly AsyncLock _mutex = new();

        /// <summary>
        /// connectionContext는 유일한 참조를 가져야 함.
        /// </summary>
        /// <param name="connectionContext"></param>
        /// <param name="sendDelay"></param>
        public User(IConnectionContext connectionContext, int sendDelay = 0)
        {
            ConnectionContext = connectionContext;
            SendDelay = sendDelay;
        }

        public User(TcpClient client, int sendDelay = 0) : this(new ConnectionContext(client), sendDelay)
        {
        }

        public async Task Run()
        {
            await Connect();

            _ = Task.Run(async () =>
            {
                while (MustBeDisconnected == false)
                {
                    try
                    {
                        await Receive();
                    }
                    catch (Exception ex)
                    {
                        Log.Print($"{ex}", LogLevel.ERROR);
                        MustBeDisconnected = true;
                    }
                }
            });

            while (MustBeDisconnected == false)
            {
                if (SendDelay > 0)
                    await Task.Delay(SendDelay);

                string? str = "";
                int connectionCheckDelay = 2000;

                try
                {
                    str = ConsoleTimeOut.ReadLine(connectionCheckDelay);
                }
                catch (Exception ex)
                {
                    //Log.Print($"{ex}", LogLevel.DEBUG);
                    continue;
                }


                if (string.IsNullOrEmpty(str))
                    continue;

                Message message = new();
                message.SetBroadcast(str);

                IPacket packet = new Utf8Packet();
                packet.Set(message);

                try
                {
                    await Send(packet);
                }
                catch (ProtocolBufferOverflowException ex)
                {
                    Log.Print($"{ex}", LogLevel.ERROR);
                    MustBeDisconnected = true;
                }
            }

            Log.Print($"{Cid} 연결 종료", LogLevel.INFO);

            try
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                Log.Print($"{Cid} 리소스 해제 실패\n{ex}", LogLevel.ERROR);
                return;
            }
            Log.Print($"{Cid} connection 리소스 해제 완료", LogLevel.INFO);
        }

        public async Task Connect(CancellationToken cancellationToken = default)
        {
            await ConnectionContext.Connect(cancellationToken);

            Message msg = new();
            msg.SetPing();

            IPacket req = new Utf8Packet();
            req.Set(msg);

            await Send(req, cancellationToken);
            IPacket res = await Receive(cancellationToken);

            Message resMsg = Serializer<Message>.Deserialize(res.GetRawString());

            if (resMsg.Type != MessageType.SUCCESS)
            {
                throw new Exception($"서버에 연결된 줄 알았으나 최초 응답을 하지 않음.\n{res.GetInfo()}");
            }
                
            MustBeDisconnected = false;
        }

        public void Disconnect()
        {
            ConnectionContext.Close();
        }

        public async Task Send(IPacket packet, CancellationToken cancellationToken = default)
        {
            var fullBytes = packet.GetFullBytes();

            Log.Print($"송신: {packet.GetInfo()}", LogLevel.INFO);

            // 비동기 송신
            await ConnectionContext.WriteAsync(fullBytes, cancellationToken);
        }

        public async Task<IPacket> Receive(CancellationToken cancellationToken = default)
        {
            byte[] sizeBytes = new byte[Utf8PayloadProtocol.SIZE_BYTES_LENGTH];
            using (await _mutex.LockAsync(cancellationToken))
            {
                int expectedMessageBytesLength = await ReceiveSize(sizeBytes, cancellationToken);
                byte[] fullBytes = new byte[Utf8PayloadProtocol.SIZE_BYTES_LENGTH + expectedMessageBytesLength];
                Buffer.BlockCopy(sizeBytes, 0, fullBytes, 0, Utf8PayloadProtocol.SIZE_BYTES_LENGTH);

                IPacket packet = await ReceiveExpect(fullBytes, expectedMessageBytesLength, cancellationToken);
                return packet;
            }
        }

        private async Task<int> ReceiveSize(byte[] fullBytes, CancellationToken cancellationToken = default)
        {
            await ReadNetworkStream(fullBytes, 0, Utf8PayloadProtocol.SIZE_BYTES_LENGTH, cancellationToken);

            return Utf8PayloadProtocol.DecodeSizeBytes(fullBytes, 0, Utf8PayloadProtocol.SIZE_BYTES_LENGTH);
        }


        private async Task<IPacket> ReceiveExpect(byte[] fullBytes, int expectedMessageBytesLength, CancellationToken cancellationToken = default)
        {
            IPacket packet = new Utf8Packet();

            int offset = Utf8PayloadProtocol.SIZE_BYTES_LENGTH;
            await ReadNetworkStream(fullBytes, offset, expectedMessageBytesLength, cancellationToken);

            packet.Set(fullBytes, offset + expectedMessageBytesLength);

            Log.Print($"수신: {packet.GetInfo()})", LogLevel.INFO);

            return packet;
        }

        private async Task ReadNetworkStream(byte[] fullBytes, int offset, int count, CancellationToken cancellationToken = default)
        {
            int receivedMessageBytesLength = 0;

            while (true)
            {
                int currentReceived = await ConnectionContext.ReadAsync(fullBytes, offset + receivedMessageBytesLength, count - receivedMessageBytesLength, cancellationToken);
                receivedMessageBytesLength += currentReceived;

                if (currentReceived == 0)
                {
                    string ex = "0 byte 수신하여 종료";
                    Log.Print(ex, LogLevel.INFO);
                    throw new IOException(ex);
                }
                if (receivedMessageBytesLength > count)
                {
                    string ex = "받기로 한 것보다 많은 메시지 바이트를 수신함";
                    Log.Print(ex, LogLevel.WARN);
                    throw new ProtocolBufferOverflowException(ex);
                }
                if (receivedMessageBytesLength < count)
                {
                    continue;
                }

                break;
            }
        }
    }
}

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
    public class User : IClient
    {
        private int SendDelay { get; set; }
        private IConnectionContext ConnectionContext { get; set; }
        private bool MustBeDisconnected { get; set; } = false;

        public bool IsReady { get { return ConnectionContext.IsReady; } }

        public string Cid { get { return ConnectionContext?.ConnectionId ?? "NA"; } }

        public string Info { get { return $"{nameof(Cid)}: {Cid}\n{nameof(SendDelay)}: {SendDelay}\n{ConnectionContext}"; } }


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
                var userInputRead = Task.Run(() => Console.ReadLine());
                int connectionCheckDelay = 2000;

                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(connectionCheckDelay);
                
                if (await Task.WhenAny(userInputRead, Task.Delay(-1, cts.Token)) != userInputRead)
                    continue;

                str = userInputRead.Result;

                if (string.IsNullOrEmpty(str))
                    continue;

                IMessage message = new Utf8Message();
                message.SetMessage(str);

                try
                {
                    await Send(message);
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

        public async Task Connect()
        {
            await ConnectionContext.Connect();
        }

        public void Disconnect()
        {
            ConnectionContext.Close();
        }

        public async Task Send(IMessage message)
        {
            var fullBytes = message.GetFullBytes();

            Log.Print($"송신: {message.GetInfo()}", LogLevel.INFO);

            // 비동기 송신
            await ConnectionContext.WriteAsync(fullBytes);
        }

        public async Task<IMessage> Receive()
        {
            byte[] fullBytes = new byte[Utf8PayloadProtocol.SIZE_BYTES_LENGTH + Utf8PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH];
            int expectedMessageBytesLength = await ReceiveSize(fullBytes);

            IMessage message = await ReceiveExpect(fullBytes, expectedMessageBytesLength);
            return message;
        }

        private async Task<int> ReceiveSize(byte[] fullBytes)
        {
            await ReadNetworkStream(fullBytes, 0, Utf8PayloadProtocol.SIZE_BYTES_LENGTH);

            return Utf8PayloadProtocol.DecodeSizeBytes(fullBytes, 0, Utf8PayloadProtocol.SIZE_BYTES_LENGTH);
        }


        private async Task<IMessage> ReceiveExpect(byte[] fullBytes, int expectedMessageBytesLength)
        {
            IMessage message = new Utf8Message();

            int offset = Utf8PayloadProtocol.SIZE_BYTES_LENGTH;
            await ReadNetworkStream(fullBytes, offset, expectedMessageBytesLength);

            message.SetBytes(fullBytes, offset + expectedMessageBytesLength);

            Log.Print($"수신: {message.GetInfo()})", LogLevel.INFO);

            return message;
        }

        private async Task ReadNetworkStream(byte[] fullBytes, int offset, int count)
        {
            int receivedMessageBytesLength = 0;

            while (true)
            {
                int currentReceived = await ConnectionContext.ReadAsync(fullBytes, offset + receivedMessageBytesLength, count - receivedMessageBytesLength);
                receivedMessageBytesLength += currentReceived;

                if (currentReceived == 0)
                {
                    string ex = "0 byte 수신하여 종료";
                    Log.Print(ex, LogLevel.INFO);
                    throw new IOException(ex);
                }
                if (receivedMessageBytesLength > count)
                {
                    string ex = "받기로 한 것보다 큰 메시지 바이트를 수신함";
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

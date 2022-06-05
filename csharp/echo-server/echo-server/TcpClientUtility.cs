using System;
using System.Threading.Tasks;


namespace chat
{
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

        public static string GetUserInput()
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
}

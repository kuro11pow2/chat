using System.Net.Sockets;
using System.Net;
using System;
using System.Threading.Tasks;
using System.Text;
using System.IO;

namespace simple_client
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

    internal class Program
    {
        static bool isConnected = true;

        static void Main(string[] args)
        {
            Log.PrintLevel = LogLevel.DEBUG;

            Log.PrintHeader();

            while (true)
            {
                try
                {
                    TcpClient tc = new TcpClient("localhost", 7000);

                    //tc.SendTimeout = 1000;
                    //tc.ReceiveTimeout = 1000;

                    NetworkStream stream = tc.GetStream();

                    AsyncTcpReceive(stream);
                    AsyncTcpSend(stream).Wait();

                    stream.Close();
                    tc.Close();
                }
                catch (Exception ex)
                {
                    Log.Print($"연결 종료\n{ex}");
                }
            }
        }

        async static Task AsyncTcpSend(NetworkStream stream)
        {

            while (isConnected)
            {
                string msg = Console.ReadLine();
                byte[] tmpBytes = PayloadEncoder.GetBytes(msg);

                if (tmpBytes.Length > PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH)
                {
                    Log.Print($"메시지가 너무 깁니다. {PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH} 이하로 입력하세요.");
                    //continue;
                }

                int msgBufferLength = tmpBytes.Length;

                byte[] sizeBytes = PayloadEncoder.Num2SizeBuffer(msgBufferLength);
                byte[] fullBuffer = new byte[PayloadEncoder.MAX_SIZE_BUFFER_LENGTH + msgBufferLength];

                Buffer.BlockCopy(sizeBytes, 0, fullBuffer, 0, sizeBytes.Length);
                Buffer.BlockCopy(tmpBytes, 0, fullBuffer, sizeBytes.Length, tmpBytes.Length);

                Log.Print($"송신: 사이즈 ({PayloadEncoder.SizeBuffer2Num(sizeBytes)}), 메시지 ({PayloadEncoder.GetString(tmpBytes, 0, tmpBytes.Length)})");

                await stream.WriteAsync(fullBuffer, 0, fullBuffer.Length);
            }
        }

        async static void AsyncTcpReceive(NetworkStream stream)
        {
            // 비동기 수신
            var msgBuff = new byte[PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH];
            string msg = "";
            int receivedMsgBuffLength = 0;

            byte[] sizeBuff = new byte[PayloadEncoder.MAX_SIZE_BUFFER_LENGTH];
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
                        currentReceived = await stream.ReadAsync(sizeBuff, sizeReceivedBuffLength, PayloadEncoder.MAX_SIZE_BUFFER_LENGTH - sizeReceivedBuffLength);
               
                        sizeReceivedBuffLength += currentReceived;
                        Log.Print($"sizeReceivedBuffLength: {sizeReceivedBuffLength}");

                        if (currentReceived == 0)
                        {
                            Log.Print("0 byte 수신하여 종료");
                            isConnected = false;
                            continue;
                        }
                        else if (sizeReceivedBuffLength < PayloadEncoder.MAX_SIZE_BUFFER_LENGTH)
                        {
                            continue;
                        }
                        else if (sizeReceivedBuffLength > PayloadEncoder.MAX_SIZE_BUFFER_LENGTH)
                        {
                            Log.Print("메세지 사이즈 버퍼 크기를 초과하여 수신함");
                            isConnected = false;
                            continue;
                        }

                        msgSize = PayloadEncoder.SizeBuffer2Num(sizeBuff);

                        if (msgSize > PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH)
                        {
                            Log.Print($"메시지 버퍼 크기를 초과하여 수신할 수 없음 : msgSize({msgSize}) > msgBufferLength({PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH})");
                            isOverflow = true;
                        }

                        break;
                    }

                    sizeReceivedBuffLength = 0;

                    while (isConnected)
                    {
                        if (isOverflow) // 메세지 버퍼 크기 초과하여 수신하려 한 케이스의 입력을 모두 제거
                        {
                            currentReceived = await stream.ReadAsync(msgBuff, 0, Math.Min(PayloadEncoder.MAX_MESSAGE_BUFFER_LENGTH, msgSize - receivedMsgBuffLength));
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

                        break;
                    }

                    receivedMsgBuffLength = 0;
                }
            }
            catch (Exception ex)
            {
                Log.Print($"연결 종료\n{ex}");
            }
        }
    }
}

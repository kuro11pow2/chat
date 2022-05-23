using System.Net.Sockets;
using System.Net;
using System;
using System.Threading.Tasks;
using System.Text;
using System.IO;

namespace simple_client
{
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
            ushort maxMsgBufferLength = 0xFFFF;
            ushort sizeBuffLength = 2;

            while (isConnected)
            {
                string msg = Console.ReadLine();
                byte[] tmpBytes = Encoding.UTF8.GetBytes(msg);

                if (tmpBytes.Length > maxMsgBufferLength)
                {
                    Log.Print($"메세지가 너무 깁니다. {maxMsgBufferLength} 이하로 입력하세요.");
                    continue;
                }

                ushort msgBufferLength = (ushort)tmpBytes.Length;

                byte[] sizeBytes = BitConverter.GetBytes(msgBufferLength)[..2];
                byte[] buff = new byte[sizeBuffLength + msgBufferLength];

                Buffer.BlockCopy(sizeBytes, 0, buff, 0, sizeBytes.Length);
                Buffer.BlockCopy(tmpBytes, 0, buff, sizeBytes.Length, tmpBytes.Length);

                await stream.WriteAsync(buff, 0, buff.Length);
            }
        }

        async static void AsyncTcpReceive(NetworkStream stream)
        {
            // 비동기 수신
            ushort msgBufferLength = 0xFFFF;
            var msgBuff = new byte[msgBufferLength];
            string msg = "";
            int receivedMsgBuffLength = 0;

            int sizeBuffLength = 2;
            byte[] sizeBuff = new byte[sizeBuffLength];
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
                        currentReceived = await stream.ReadAsync(sizeBuff, sizeReceivedBuffLength, sizeBuffLength - sizeReceivedBuffLength);
                        sizeReceivedBuffLength += currentReceived;
                        Log.Print($"sizeReceivedBuffLength: {sizeReceivedBuffLength}");

                        if (currentReceived == 0)
                        {
                            Log.Print("0 byte 수신하여 종료");
                            isConnected = false;
                            continue;
                        }
                        else if (sizeReceivedBuffLength < sizeBuffLength)
                        {
                            continue;
                        }
                        else if (sizeReceivedBuffLength > sizeBuffLength)
                        {
                            Log.Print("메세지 사이즈 버퍼 크기를 초과하여 수신함");
                            isConnected = false;
                            continue;
                        }

                        msgSize = BitConverter.ToInt16(sizeBuff);

                        if (msgSize > msgBufferLength)
                        {
                            Log.Print($"메시지 버퍼 크기를 초과하여 수신할 수 없음 : msgSize({msgSize}) > msgBufferLength({msgBufferLength})");
                            isOverflow = true;
                        }

                        break;
                    }

                    sizeReceivedBuffLength = 0;

                    while (isConnected)
                    {
                        if (isOverflow) // 메세지 버퍼 크기 초과하여 수신하려 한 케이스의 입력을 모두 제거
                        {
                            currentReceived = await stream.ReadAsync(msgBuff, 0, Math.Min(msgBufferLength, msgSize - receivedMsgBuffLength));
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

                        msg = Encoding.UTF8.GetString(msgBuff, 0, msgSize);

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

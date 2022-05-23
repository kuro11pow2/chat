﻿using System.Net.Sockets;
using System.Net;
using System;
using System.Threading.Tasks;
using System.Text;
using System.IO;


namespace echo_server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.PrintLevel = LogLevel.DEBUG;

            Log.PrintHeader();

            AysncEchoServer().Wait();
        }

        async static Task AysncEchoServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 7000);
            listener.Start();
            while (true)
            {
                // 비동기 Accept                
                TcpClient tc = await listener.AcceptTcpClientAsync();

                //tc.SendTimeout = 1000;
                //tc.ReceiveTimeout = 1000;

                // 새 쓰레드에서 처리
                _ = Task.Run(() =>
                {
                    AsyncTcpEcho(tc);
                });
            }
        }

        async static void AsyncTcpEcho(TcpClient tc)
        {
            NetworkStream stream = tc.GetStream();

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

                        byte[] fullBuff = new byte[sizeBuff.Length + msgSize];
                        Buffer.BlockCopy(sizeBuff, 0, fullBuff, 0, sizeBuff.Length);
                        Buffer.BlockCopy(msgBuff, 0, fullBuff, sizeBuff.Length, msgSize);

                        // 비동기 송신
                        await stream.WriteAsync(fullBuff, 0, fullBuff.Length);

                        break;
                    }

                    receivedMsgBuffLength = 0;
                }

                stream.Close();
                tc.Close();
            }
            catch (Exception ex)
            {
                Log.Print($"연결 종료\n{ex}");
            }
        }

    }
}

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace chat_client
{
    class Program
    {
        const ushort MAX_SIZE = 1024;
        const ushort PORT_NUM = 1234;
        const string TARGET_ADDRESS = "localhost";

        static void WriteLog(string msg)
        {
            Console.WriteLine($"{DateTime.Now.ToString("mm.ss.ffff")}: [{Thread.CurrentThread.ManagedThreadId}] {msg}");
        }
        static void Main(string[] args)
        {
            TcpClient tc = new TcpClient(TARGET_ADDRESS, PORT_NUM);
            WriteLog("tcp 클라 생성");


            NetworkStream stream = tc.GetStream();
            WriteLog("tcp 클라 네트워크 스트림 얻기");


            string msg = $"나는 클라{Process.GetCurrentProcess().Id}";
            byte[] buff = Encoding.UTF8.GetBytes(msg);
            stream.Write(buff, 0, buff.Length);
            WriteLog($"네트워크 스트림에 바이트 데이터 '{msg}' 쓰기");


            byte[] outbuf = new byte[MAX_SIZE];
            int nbytes;
            MemoryStream mem = new MemoryStream();
            while ((nbytes = stream.Read(outbuf, 0, outbuf.Length)) > 0)
            {
                mem.Write(outbuf, 0, nbytes);
                WriteLog($"네트워크 스트림에서 {nbytes} 읽고 메모리 스트림에 기록");
            }
            byte[] outbytes = mem.ToArray();
            WriteLog($"메모리 스트림 내용을 배열에 저장");
            mem.Close();
            WriteLog($"메모리 스트림 닫기");


            stream.Close();
            WriteLog($"네트워크 스트림 닫기");
            tc.Close();
            WriteLog($"tcp 클라 닫기");

            WriteLog($"(읽어온 메시지) {Encoding.UTF8.GetString(outbytes)}");
        }
    }
}
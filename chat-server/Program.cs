using System.Net.Sockets;
using System.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;


namespace chat_server
{
    class Program
    {
        const ushort MAX_SIZE = 1024;
        const ushort PORT_NUM = 1234;

        static void WriteLog(string msg)
        {
            Console.WriteLine($"{DateTime.Now.ToString("mm.ss.ffff")}: [{Thread.CurrentThread.ManagedThreadId}] {msg}");
        }
        async static void AsyncTcpAction(object o)
        {
            TcpClient tc = (TcpClient)o;
            WriteLog($"tcp 클라를 인자로 전달받음");

            NetworkStream stream = tc.GetStream();
            WriteLog($"tcp 클라에서 네트워크 스트림을 받아옴");

            var buff = new byte[MAX_SIZE];
            var nbytes = await stream.ReadAsync(buff, 0, buff.Length).ConfigureAwait(false);
            WriteLog($"네트워크 스트림에서 {nbytes}byte 읽음");

            if (nbytes > 0)
            {
                string msg = Encoding.UTF8.GetString(buff, 0, nbytes);
                WriteLog($"(읽어온 메시지) {msg}");

                string ret_msg = "나는 서버";
                byte[] ret_buff = Encoding.UTF8.GetBytes(ret_msg);
                await stream.WriteAsync(ret_buff, 0, ret_buff.Length).ConfigureAwait(false);
                WriteLog($"네트워크 스트림에 바이트 데이터 '{ret_msg}' 쓰기");
            }

            stream.Close();
            WriteLog($"네트워크 스트림 닫기");
            tc.Close();
            WriteLog($"tcp 클라 닫기");
        }
        async static Task AsyncTcpServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, PORT_NUM);
            WriteLog($"tcp listener 생성");

            listener.Start();
            WriteLog($"tcp listener 실행");

            while (true)
            {
                TcpClient tc = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                WriteLog($"tcp 요청 accept 하고 tcp 클라 생성");

                Task.Factory.StartNew(AsyncTcpAction, tc);
                WriteLog($"다른 스레드에서 AsyncTcpAction 호출하고 tcp 클라 전달");
            }
        }
        static void Main(string[] args)
        {
            WriteLog($"서버 시작");
            AsyncTcpServer().Wait();
            WriteLog($"서버 종료");
        }
    }
}


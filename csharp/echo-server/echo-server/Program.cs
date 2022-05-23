using System.Net.Sockets;
using System.Net;
using System;
using System.Threading.Tasks;
using System.Text;
using System.IO;


namespace echo_server
{
    internal class Program
    {
        async static Task AysncEchoServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 7000);
            listener.Start();
            while (true)
            {
                // 비동기 Accept                
                TcpClient tc = await listener.AcceptTcpClientAsync();

                // 새 쓰레드에서 처리
                _ = Task.Run(() =>
                {
                    AsyncTcpProcess(tc);
                });
            }
        }

        async static void AsyncTcpProcess(object o)
        {
            TcpClient tc = (TcpClient)o;

            int MAX_SIZE = tc.ReceiveBufferSize;
            NetworkStream stream = tc.GetStream();


            // 비동기 수신            
            var buff = new byte[MAX_SIZE];

            while (true)
            {
                try
                {
                    int nbytes = await stream.ReadAsync(buff, 0, buff.Length);

                    if (nbytes > 0)
                    {
                        string msg = Encoding.ASCII.GetString(buff, 0, nbytes);
                        Log.Print($"{msg} at {DateTime.Now}");

                        // 비동기 송신
                        await stream.WriteAsync(buff, 0, nbytes);
                    }
                    else
                    {
                        Log.Print("0 byte 수신하여 종료");
                        break;
                    }
                }
                catch (IOException ex)
                {
                    Log.Print($"\n{ex}");
                }
            }

            stream.Close();
            tc.Close();
        }

        static void Main(string[] args)
        {
            Log.PrintLevel = LogLevel.DEBUG;

            Log.PrintHeader();

            AysncEchoServer().Wait();
        }
    }
}

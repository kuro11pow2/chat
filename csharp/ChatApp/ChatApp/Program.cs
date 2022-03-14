using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Encodings;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ChatApp
{
    class Program
    {
        static int port = 54321;
        static int backlog = 10;
        static string listenerAddress = "127.0.0.1";
        public static async void RunMonitor(Server server)
        {
            while (true)
            {
                Log.Print(server.Status(), ChatLogLevel.RELEASE);
                await Task.Delay(5000);
            }
        }
        public static void RunServer()
        {
            Server server = new Server(port, backlog);
            server.Init();
            server.AcceptAndRegister();
            //RunMonitor(server);
        }
        public static async void RunClients(int clientCount)
        {
            for (int i = 0; i < clientCount; i++)
            {
                Client client = new Client(i, listenerAddress, port);
                await client.Init();

                Task _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        await client.SendAsync("hi");
                        await Task.Delay(1000 * clientCount);
                    }
                });
            }
        }
        public static async Task RunUserClient()
        {
            Client client = new Client(0, listenerAddress, port);
            await client.Init();

            Task _ = Task.Run(async () =>
            {
                while (true)
                    await client.ReceiveAsync();
            });

            string input;
            while ((input = Console.ReadLine()) != "exit")
            {
                await client.SendAsync(input);
            }
        }
        public static async Task Main(string[] args)
        {
            ThreadPool.SetMaxThreads(16, 1000);
            Log.PrintLevel = ChatLogLevel.RELEASE;

            //Log.PrintHeader();

            RunServer();
            RunClients(10);

            //await RunUserClient();
        }
    }
}



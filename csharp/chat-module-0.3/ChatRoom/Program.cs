using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Common.Utility;
using Common.Interface;

namespace Chat
{
    public class Object2FileHelper<T>
    {
        private string FilePath;
        private JsonSerializerOptions Options = new() { WriteIndented = true, IncludeFields = true, };

        public Object2FileHelper(string filePath)
        {
            FilePath = filePath;
        }

        public async Task Save(T obj)
        {
            using FileStream createStream = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(createStream, obj, Options);
            await createStream.DisposeAsync();
        }

        public async Task<T?> Load()
        {
            using FileStream openStream = File.OpenRead(FilePath);
            T? obj = await JsonSerializer.DeserializeAsync<T>(openStream);
            return obj;
        }
    }

    class ThreadPoolUtility
    {
        public static void SetThreadCount(int worker, int completionPort)
        {
            ThreadPool.SetMinThreads(worker, completionPort);
        }
        public static string GetThreadPoolInfo()
        {
            int max_worker, min_worker, avail_worker;
            int max_completion_port, min_completion_port, avail_completion_port;
            ThreadPool.GetMaxThreads(out max_worker, out max_completion_port);
            ThreadPool.GetMinThreads(out min_worker, out min_completion_port);
            ThreadPool.GetAvailableThreads(out avail_worker, out avail_completion_port);
            return $"[{nameof(GetThreadPoolInfo)}]\n{nameof(max_worker)}: {max_worker}\n{nameof(min_worker)}: {min_worker}\n{nameof(avail_worker)}: {avail_worker}\n{nameof(max_completion_port)}: {max_completion_port}\n{nameof(min_completion_port)}: {min_completion_port}\n{nameof(avail_completion_port)}: {avail_completion_port}";
        }
    }

    internal class Program
    {
        static async Task ChatRoomTest()
        {
            Log.Print($"ChatClientTest 테스트 시작");

            List<(string, string)> parameters = new List<(string, string)>
            {
                (@"01234", @"01234"),
                (@"abcxyz", @"abcxyz"),
                (@"@!#$%^()[]", @"@!#$%^()[]"),
                (@"가나다뀕팛", @"가나다뀕팛"),
                (@"凰猫天䬌", @"凰猫天䬌"),
                (@"😂🤣⛴🛬🎁", @"😂🤣⛴🛬🎁"),
            };

            int port = 1234;
            Room room = new Room(port);
            List<Client> clients = new List<Client>();
            for (int i = 0; i < 3; i++)
            {
                clients.Add(new Client("localhost", port, i));
            }

            _ = room.Run();
            _ = room.RunMonitor();

            foreach (var client in clients)
            {
                await client.Connect();
            }

            foreach (var parameter in parameters)
            {
                var input = parameter.Item1;
                var expected = parameter.Item2;

                await clients[0].Send(input);

                foreach (var client in clients)
                {
                    var output = await client.Receive();
                    Debug.Assert(output == expected, $"테스트 실패, input: {input}, output: {output}, expected: {expected}");
                }
            }

            Log.Print($"ChatClientTest 테스트 통과, 테스트케이스 수: {parameters.Count}");

            
        }

        static async Task Main(string[] args)
        {
            Log.PrintHeader();
            await ChatRoomTest();
            Console.ReadLine();
        }
    }
}
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;


namespace chat
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

        public async Task<T> Load()
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
        static Config Config = new Config();
        static Object2FileHelper<Config> Config2FileHelper;

        static async Task Main(string[] args)
        {
#if DEBUG
#else
            string fileName = "config.json";
            Config2FileHelper = new Object2FileHelper<Config>(fileName);
            try
            {
                Config = await Config2FileHelper.Load();
            }
            catch (FileNotFoundException ex)
            {
                await Config2FileHelper.Save(Config);
            }
#endif
            Config.PrintLevel = LogLevel.WARN;

            Log.PrintHeader();
            Log.Print($"\n{Config}", LogLevel.INFO);
            Log.PrintLevel = Config.PrintLevel;

            Server server = new Server(Config.Port);
            _ = server.RunMonitor();
            await server.Run();
        }
    }
}

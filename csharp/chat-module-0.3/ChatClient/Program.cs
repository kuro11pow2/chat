using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System;

using System.Net.Sockets;
using Common.Utility;
using Common.Interface;
using Common;

namespace Chat
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Log.PrintHeader();
#if RELEASE
            const string filePath = "user_config.json";
            Object2File<Config> object2FileHelper = new Object2File<Config>(filePath);
            Config config;

            try
            {
                config = await object2FileHelper.Load();
            }
            catch (FileNotFoundException ex)
            {
                config = new Config();
                config.ServerAddress = "192.168.0.53";
                await object2FileHelper.Save(config);
            }
#elif DEBUG || TEST
            Config config = new();
#endif
            Log.PrintLevel = config.PrintLevel;
            User user = new(new ConnectionContext(config.ServerAddress, config.Port));

            while (true)
            {
                try
                {
                    await user.Run();
                }
                catch 
                {
                    Log.Print($"서버 연결 시도중\n{user.Info}", LogLevel.ERROR);
                }
                await Task.Delay(1000);
            }
        }
    }
}
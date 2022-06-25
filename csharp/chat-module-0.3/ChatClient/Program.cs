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
            Object2FileHelper<Config> object2FileHelper = new Object2FileHelper<Config>(filePath);
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
#elif DEBUG
            Config config = new Config();
#endif
            Log.PrintLevel = config.PrintLevel;
            User user = new User(config.ServerAddress, config.Port);

            while (true)
            {
                await user.Run();
                await Task.Delay(5000);
            }
        }
    }
}
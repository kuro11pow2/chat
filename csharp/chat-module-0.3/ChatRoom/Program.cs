using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

using Common;
using Common.Utility;
using Common.Interface;

namespace Chat
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            Log.PrintHeader();

#if RELEASE 
            const string filePath = "room_config.json";
            Object2FileHelper<Config> object2FileHelper = new Object2FileHelper<Config>(filePath);
            Config config;

            try
            {
                config = await object2FileHelper.Load();
            }
            catch (FileNotFoundException ex)
            {
                config = new Config();
                await object2FileHelper.Save(config);
            }
#elif DEBUG || TEST
            Config config = new();
#endif
            Log.PrintLevel = config.PrintLevel;
            RoomQ room = new(config.Port);

            //_ = room.RunMonitor();
            _ = room.UserCommand();
            await room.Run();
        }
    }
}

using System.Reflection;

namespace Chat
{
    using Common;
    using Common.Interface;
    using Common.Utility;
    using Chat;

    public static class ProcessUserInput
    {
        public static bool Run(RoomQ room, int checkDelay)
        {
            bool commandResult = false;
            string commandResultStr = "잘못된 입력입니다";

            GetUserInput(checkDelay, out bool isTimeout, out string[]? tokens);
            if (isTimeout) return true;
            if (tokens != null)
            {
                Dictionary<string, Action> commands = new()
                {
                    { "close", () => RunClose(tokens, room, out commandResult, out commandResultStr) },
                    { "info", () => RunInfo(tokens, room, out commandResult, out commandResultStr) },
                    { "kick", () => RunKick(tokens, room, out commandResult, out commandResultStr) },
                    { "loglevel", () => RunLogLevel(tokens, out commandResult, out commandResultStr) },
                    { "broadcast", () => RunBroadcast(tokens, room, out commandResult, out commandResultStr) },
                };
                if (tokens[0] == "help") RunHelp(tokens, commands, out commandResult, out commandResultStr);
                else if (commands.TryGetValue(tokens[0], out Action? action)) action();
            }

            Log.Print(commandResultStr, LogLevel.RETURN, "command result");
            return commandResult;
        }

        private static void GetUserInput(int checkDelay, out bool isTimeout, out string[]? tokens)
        {
            string? inputStr;
            tokens = null;
            isTimeout = false;

            try
            {
                inputStr = ConsoleTimeOut.ReadLine(checkDelay);
            }
            catch (Exception ex)
            {
                isTimeout = true;
                return;
            }

            inputStr = inputStr?.Trim();
            tokens = inputStr?.Split(" ");
        }
        private static void RunHelp(string[] tokens, Dictionary<string, Action> commands, out bool res, out string resStr)
        {
            const int tokenCount = 1;
            if (tokens.Length != tokenCount)
            {
                res = false;
                resStr = $"인자의 개수는 {tokenCount}개여야 합니다";
                return;
            }

            res = true;
            resStr = $"도움말 출력\n[명령어 목록]\n{string.Join("\n", commands.Keys.ToArray())}";
        }

        private static void RunClose(string[] tokens, RoomQ room, out bool res, out string resStr)
        {
            const int tokenCount = 1;
            if (tokens.Length != tokenCount)
            {
                res = false;
                resStr = $"인자의 개수는 {tokenCount}개여야 합니다";
                return;
            }

            room.Close();

            res = true;
            resStr = $"방 닫기";
        }

        private static void RunInfo(string[] tokens, RoomQ room, out bool res, out string resStr)
        {
            const int tokenCount = 1;
            if (tokens.Length != tokenCount)
            {
                res = false;
                resStr = $"인자의 개수는 {tokenCount}개여야 합니다";
                return;
            }

            Assembly thisAssem = typeof(RoomQ).Assembly;
            AssemblyName thisAssemName = thisAssem.GetName();
            Version? ver = thisAssemName.Version;

            res = true;
            resStr = $"정보 출력\n[Room Info]\n{room.Info()}\n\n[Config]\n{nameof(Log.PrintLevel)}: {Log.PrintLevel}\n{nameof(RoomQ)} Version: {thisAssemName.Name}-{ver}, Built Time: {BuildVersion2DateTime.Get(ver)}";
        }
        private static void RunKick(string[] tokens, RoomQ room, out bool res, out string resStr)
        {
            const int tokenCount = 2;
            if (tokens.Length != tokenCount)
            {
                res = false;
                resStr = $"인자의 개수는 {tokenCount}개여야 합니다";
                return;
            }
            if (string.IsNullOrWhiteSpace(tokens[1]))
            {
                res = false;
                resStr = "#1 인자의 값은 null이나 공백일 수 없습니다";
                return;
            }

            room.Kick(tokens[1]);

            res = true;
            resStr = $"{tokens[1]} 강퇴";
        }

        private static void RunLogLevel(string[] tokens, out bool res, out string resStr)
        {
            string noti = $"[입력 가능한 값]\n{string.Join(", ", Enum.GetNames(typeof(LogLevel))[1..])}";

            const int tokenCount = 2;
            if (tokens.Length != tokenCount)
            {
                res = false;
                resStr = $"인자의 개수는 {tokenCount}개여야 합니다\n{noti}";
                return;
            }

            if (string.IsNullOrWhiteSpace(tokens[1]))
            {
                res = false;
                resStr = $"#1 인자의 값은 null이나 공백일 수 없습니다\n{noti}";
                return;
            }

            if (int.TryParse(tokens[1], out int result) == true)
            {
                res = false;
                resStr = $"#1 인자의 값은 int일 수 없습니다\n{noti}";
                return;
            }

            if (Enum.TryParse(typeof(LogLevel), tokens[1], out object? level) == false)
            {
                res = false;
                resStr = $"#1 인자의 값을 {nameof(LogLevel)}로 파싱할 수 없습니다\n{noti}";
                return;
            }

            if (level == null)
            {
                res = false;
                resStr = $"#1 인자의 값을 {nameof(LogLevel)}로 파싱한 결과는 null일 수 없습니다\n{noti}";
                return;
            }

            Log.PrintLevel = (LogLevel)level;

            res = true;
            resStr = $"로그 레벨 {Log.PrintLevel}로 변경";
        }

        private static void RunBroadcast(string[] tokens, RoomQ room, out bool res, out string resStr)
        {
            const int tokenCount = 2;
            if (tokens.Length != tokenCount)
            {
                res = false;
                resStr = $"인자의 개수는 {tokenCount}개여야 합니다";
                return;
            }

            if (string.IsNullOrWhiteSpace(tokens[1]))
            {
                res = false;
                resStr = $"#1 인자의 값은 null이나 공백일 수 없습니다";
                return;
            }

            Message message = new();
            message.SetBroadcast(tokens[1]);

            IPacket packet = new Utf8Packet();
            packet.Set(message);

            room.Broadcast(new User(new ConnectionContext("host broadcast", -1)), packet);

            res = true;
            resStr = $"브로드캐스트\n{message.GetInfo()}";
        }
    }
}

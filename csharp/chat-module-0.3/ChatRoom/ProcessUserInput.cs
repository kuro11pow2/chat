using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

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
            GetUserInput(checkDelay, out bool isTimeout, out string[]? tokens);

            bool commandResult = false;
            string commandResultStr = "잘못된 입력입니다";

            if (isTimeout) return true;
            else if (tokens?[0] == "close") RunClose(tokens, room, out commandResult, out commandResultStr);
            else if (tokens?[0] == "info") RunInfo(tokens, room, out commandResult, out commandResultStr);
            else if(tokens?[0] == "kick") RunKick(tokens, room, out commandResult, out commandResultStr);
            else if(tokens?[0] == "loglevel") RunLogLevel(tokens, out commandResult, out commandResultStr);
            else if(tokens?[0] == "broadcast") RunBroadcast(tokens, room, out commandResult, out commandResultStr);

            Log.Print(commandResultStr, LogLevel.RETURN, "명령 결과");
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

            res = true;
            resStr = $"[방 정보]\n{room.Info()}\n\n[설정 정보]\n{nameof(Log.PrintLevel)}: {Log.PrintLevel}";
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
            const int tokenCount = 3;
            if (tokens.Length != tokenCount)
            {
                res = false;
                resStr = $"인자의 개수는 {tokenCount}개여야 합니다";
                return;
            }

            if (string.IsNullOrWhiteSpace(tokens[1]) && string.IsNullOrWhiteSpace(tokens[2]))
            {
                res = false;
                resStr = $"#1 또는 #2 인자의 값은 null이나 공백일 수 없습니다";
            }

            Utf8Message message = new();
            message.SetMessage(tokens[2]);

            room.Broadcast(tokens[1], message);

            res = true;
            resStr = $"브로드캐스트\n{message.GetInfo()}";
        }
    }
}

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
        public static void Run(RoomQ room, int checkDelay)
        {
            string? str;
            try
            {
                str = ConsoleTimeOut.ReadLine(checkDelay);
            }
            catch (Exception ex)
            {
                //Log.Print($"{ex}", LogLevel.DEBUG);
                return;
            }

            if (string.IsNullOrWhiteSpace(str))
                return;

            str = str.Trim();
            string[] tokens = str.Split(" ");

            if (tokens.Length == 1)
            {
                if (tokens[0] == "close")
                {
                    Log.Print($"방 제거", LogLevel.INFO, "명령 결과");
                    room.Close();
                    return;
                }
                if (tokens[0] == "info")
                {
                    Log.Print(room.Info(), LogLevel.INFO, "명령 결과");
                    return;
                }
            }
            else if (tokens.Length == 2)
            {
                if (tokens[0] == "kick")
                {
                    if (string.IsNullOrWhiteSpace(tokens[1]))
                        return;

                    Log.Print($"강퇴", LogLevel.INFO, "명령 결과");
                    room.Kick(tokens[1]);
                    return;
                }
            }
            else if (tokens.Length == 3)
            {
                if (tokens[0] == "broadcast")
                {
                    if (string.IsNullOrWhiteSpace(tokens[1]) && string.IsNullOrWhiteSpace(tokens[2]))
                        return;

                    Utf8Message message = new();
                    message.SetMessage(tokens[2]);

                    Log.Print($"브로드캐스트", LogLevel.INFO, "명령 결과");
                    room.Broadcast(tokens[1], message);
                    return;
                }
            }

            Log.Print("잘못된 입력입니다.", LogLevel.INFO, "명령 결과");
        }
    }
}

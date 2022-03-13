using System;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace ChatApp
{
    /// <summary>
    /// 커질 수록 세세한 부분까지 출력
    /// </summary>
    public enum ChatLogLevel
    {
        /// <summary>
        /// 배포 시에도 출력할 특수한 로그
        /// </summary>
        RELEASE,
        /// <summary>
        /// 예외
        /// </summary>
        EXCEPTION,
        /// <summary>
        /// 유저 수준 기본 출력
        /// </summary>
        DEFAULT,
        /// <summary>
        /// 알아야 하는 정보
        /// </summary>
        INFO,
        /// <summary>
        /// 디버그에 사용하는 정보
        /// </summary>
        DEBUG,
        /// <summary>
        /// 모든 정보
        /// </summary>
        VERBOSE,
    }

    public class Log
    {
        public static ChatLogLevel PrintLevel = ChatLogLevel.INFO;
        public const int Width = 3;
        public delegate void OutStream(string obj);
        public static OutStream Out = Console.WriteLine;
        private static string _line = "--------------------------------------------------------------------------------------------------------------------";

        /// <summary>
        /// 디버그 모드에서만 메소드 호출된다던데, 파라미터는 평가되는건지 성능 비교를 통해 파악해보기
        /// 디버그 vs 릴리즈
        /// 출력 없는 디버그 vs 출력 없는 릴리즈
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="level"></param>
        /// <param name="context"></param>
        //[Conditional("DEBUG")] NsLog를 여기에 매핑할 거라 함수 살아있어야 함. 그래서 주석처리
        public static void Print(string msg, ChatLogLevel level = ChatLogLevel.DEFAULT, string context = "")
        {
            if (level <= PrintLevel)
            {
                string printMsg;
                printMsg = FormattedString(
                    DateTime.Now.ToString("mm.ss.ffff"),
                    ThreadPool.ThreadCount,
                    ThreadPool.PendingWorkItemCount,
                    Thread.CurrentThread.ManagedThreadId,
                    context,
                    LogLevelTag(level),
                    $"{msg}\n{_line}");
                Out(printMsg);
            }
        }
        public static string LogLevelTag(ChatLogLevel level)
        {
            switch (level)
            {
                case ChatLogLevel.RELEASE:
                    return "RELEASE";
                case ChatLogLevel.EXCEPTION:
                    return "EXCEPTION";
                case ChatLogLevel.DEFAULT:
                    return "";
                case ChatLogLevel.INFO:
                    return "INFO";
                case ChatLogLevel.DEBUG:
                    return "DEBUG";
                case ChatLogLevel.VERBOSE:
                    return "VERBOSE";
                default:
                    return "WRONG LEVEL";
            }
        }

        //[Conditional("DEBUG")]
        public static void PrintLine()
        {
            Out(_line);
        }

        //[Conditional("DEBUG")]
        public static void PrintHeader()
        {
            if (PrintLevel > ChatLogLevel.DEFAULT)
                Out(FormattedString(
                    "mm.ss.ffff",
                    "Avail",
                    "Pend",
                    "TID",
                    "Context",
                    "Type",
                    "Message") + $"\n{_line}");
        }

        public static string FormattedString(object time, object tall, object tpend, object tid, object contect, object type, object msg)
        {
            return $"| {time,-9} | {tall,-5} | {tpend,-4} | {tid,-4} | {contect,-34} | {type,-9} | {msg,-1}";
        }
    }
}
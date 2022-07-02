using System;
using System.Threading;

namespace Common.Utility
{
    public enum LogLevel
    {
        OFF,
        RETURN,
        ERROR,
        WARN,
        INFO,
        DEBUG
    }

    public static class Log
    {
        public static LogLevel PrintLevel = LogLevel.DEBUG;
        private const int Width = 3;
        private static readonly string _line = "--------------------------------------------------------------------------------------------------------------------";

        private static void Out(object obj)
        {
#if !TEST
            Console.WriteLine(obj);
#endif
        }

        /// <summary>
        /// 디버그 모드에서만 메소드 호출된다던데, 파라미터는 평가되는건지 성능 비교를 통해 파악해보기
        /// 디버그 vs 릴리즈
        /// 출력 없는 디버그 vs 출력 없는 릴리즈
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="level"></param>
        /// <param name="context"></param>
        //[Conditional("DEBUG")]
        public static void Print(string msg, LogLevel level = LogLevel.DEBUG, string context = "")
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
        private static string LogLevelTag(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.RETURN:
                    return "RETURN";
                case LogLevel.ERROR:
                    return "ERROR";
                case LogLevel.WARN:
                    return "WARM";
                case LogLevel.INFO:
                    return "INFO";
                case LogLevel.DEBUG:
                    return "DEBUG";
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
            if (PrintLevel > LogLevel.ERROR)
                Out(FormattedString(
                    "mm.ss.ffff",
                    "Avail",
                    "Pend",
                    "TID",
                    "Context",
                    "Type",
                    "Message") + $"\n{_line}");
        }

        private static string FormattedString(object time, object tall, object tpend, object tid, object contect, object type, object msg)
        {
            return $"| {time,-9} | {tall,-5} | {tpend,-4} | {tid,-4} | {contect,-34} | {type,-9} | {msg,-1}";
        }
    }
}

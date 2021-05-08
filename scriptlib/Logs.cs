using System;
using System.Threading.Tasks;

using metastrings;

namespace metascript
{
    public enum LogLevel
    {
        ERROR = 1,
        INFO,
        TRACE
    }

    public static class Logs
    {
        public static bool ShouldSkip(LogLevel level)
        {
            EnsureInit();
            return level == LogLevel.TRACE && !sm_logTrace;
        }

        public static async Task LogAsync(Context ctxt, LogLevel level, string msg)
        {
            if (ShouldSkip(level)) // calls EnsureInit
                return;

            Console.WriteLine($"{Enum.GetName(typeof(LogLevel), level)}: {msg}");

            if (level == LogLevel.ERROR)
            {
                await ErrorLog.LogAsync(ctxt, msg).ConfigureAwait(false);
            }
            else
            {
                msg = TrimMsg(msg);

                var define = new Define("userlogs", Guid.NewGuid().ToString());
                define.Set("logdate", DateTime.UtcNow.ToString("o"));
                define.Set("loglevel", level);
                define.Set("msg", msg);
                await ctxt.Cmd.DefineAsync(define).ConfigureAwait(false);
            }
        }

        private static string TrimMsg(string msg)
        {
            if (msg.Length <= cMaxLogMsgLen)
                return msg;
            else
                return msg.Substring(0, cMaxLogMsgLen - "...".Length) + "...";
        }
        private const int cMaxLogMsgLen = 256 - 1;

        private static void EnsureInit()
        {
            if (sm_bInitted)
                return;
            lock (sm_initLock)
            {
                if (sm_bInitted)
                    return;

                sm_logTrace = MUtils.GetAppSettingBool("LogTrace");
                sm_bInitted = true;
            }
        }
        private static bool sm_bInitted = false;
        private static object sm_initLock = new object();

        private static bool sm_logTrace;
    }
}

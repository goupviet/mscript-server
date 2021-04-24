using System;
using System.Threading.Tasks;

using metastrings;

namespace metascript
{
    public enum LogLevel
    {
        ERROR = 1,
        WARNING,
        INFO,
        TRACE
    }

    public class LogContext
    {
        public long userId;
        public string ip;
    }

    public static class Logs
    {
        public static async Task LogTraceAsync(Context ctxt, string msg, LogContext logCtxt)
        {
            await LogAsync(ctxt, LogLevel.TRACE, msg, logCtxt);
        }

        public static async Task LogInfoAsync(Context ctxt, string msg, LogContext logCtxt)
        {
            await LogAsync(ctxt, LogLevel.INFO, msg, logCtxt);
        }

        public static bool ShouldSkip(LogLevel level)
        {
            EnsureInit();
            return level == LogLevel.TRACE && !sm_logTrace;
        }

        public static async Task LogAsync(Context ctxt, LogLevel level, string msg, LogContext logCtxt)
        {
            EnsureInit();
            if (level == LogLevel.TRACE && !sm_logTrace)
                return;
#if DEBUG
            Console.WriteLine($"{Enum.GetName(typeof(LogLevel), level)}: {msg}");
#endif
            if (level == LogLevel.ERROR)
                await LogErrorAsync(ctxt, msg, logCtxt);

            msg = TrimMsg(msg);

            var define = new Define("userlogs", Guid.NewGuid().ToString());
            define.Set("logdate", DateTime.UtcNow.ToString("o"));
            define.Set("loglevel", level);
            define.Set("userid", logCtxt.userId);
            define.Set("ip", logCtxt.ip);
            define.Set("msg", msg);
            await ctxt.Cmd.DefineAsync(define);
        }

        // for use in exception handlers where async is disallowed
        public static async Task LogErrorAsync(Context ctxt, string msg, LogContext logCtxt) 
        {
            EnsureInit();
            await ErrorLog.LogAsync(ctxt, logCtxt.userId, logCtxt.ip, TrimErrorMsg(msg));
        }

        private static string TrimMsg(string msg)
        {
            if (msg.Length <= cMaxLogMsgLen)
                return msg;
            else
                return msg.Substring(0, cMaxLogMsgLen - "...".Length) + "...";
        }

        private static string TrimErrorMsg(string msg)
        {
            if (msg.Length <= cMaxErrorgMsgLen)
                return msg;
            else
                return msg.Substring(0, cMaxErrorgMsgLen - "...".Length) + "...";
        }

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

        private const int cMaxLogMsgLen = 256 - 1;
        private const int cMaxErrorgMsgLen = 64 * 1024 - 1;
    }
}

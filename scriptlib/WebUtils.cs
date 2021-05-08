using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace metascript
{
    public static class WebUtils
    {
        public static string GetField(string key, Dictionary<string, string> dict)
        {
            string val;
            if (dict == null || !dict.TryGetValue(key, out val))
                val = "";
            return val;
        }

        public static async Task LogInfoAsync(HttpState state, string msg)
        {
            await LogAsync(state, LogLevel.INFO, msg).ConfigureAwait(false);
        }

        public static async Task LogTraceAsync(HttpState state, string msg, params object [] args)
        {
            if (Logs.ShouldSkip(LogLevel.TRACE))
                return;

            msg = string.Format(msg, args);
            await LogAsync(state, LogLevel.TRACE, msg).ConfigureAwait(false);
        }

        public static async Task HandleUserErrorAsync(HttpState state, UserException exp)
        {
            string errorInfo = Errors.GetErrorInfo(state);
            await ErrorLog.LogAsync(state.MsCtxt, $"User Exception: {errorInfo}: {exp.Message}").ConfigureAwait(false);
            await state.SetFinalStatusAsync(400, exp.Message).ConfigureAwait(false);
        }

        private static async Task LogAsync(HttpState state, LogLevel level, string msg)
        {
            await Logs.LogAsync(state.MsCtxt, level, msg).ConfigureAwait(false);
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using metastrings;

namespace metascript
{
    /// <summary>
    /// How do we represent a single log entry?
    /// </summary>
    public class ErrorLogEntry
    {
        public DateTime when { get; set; }
        public string msg { get; set; }
    }

    /// <summary>
    /// Log error message to the metastrings errorlog table with this class
    /// </summary>
    public static class ErrorLog
    {
        /// <summary>
        /// Log the error message
        /// </summary>
        /// <param name="ctxt">Database connection</param>
        /// <param name="msg">The error message</param>
        public static async Task LogAsync(Context ctxt, string msg)
        {
            msg = TrimErrorMsg(msg);

            string logKey = Guid.NewGuid().ToString();

            Define define = new Define("errorlog", logKey);
            define.Set("when", DateTime.UtcNow.ToString("o"));
            await ctxt.Cmd.DefineAsync(define).ConfigureAwait(false);

            long logId = await ctxt.GetRowIdAsync("errorlog", logKey).ConfigureAwait(false);

            await ctxt.Cmd.PutLongStringAsync
            (
                new LongStringPut()
                {
                    table = "errorlog",
                    fieldName = "msg",
                    itemId = logId,
                    longString = msg
                }
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Query for error messages
        /// </summary>
        /// <param name="ctxt">Database connection</param>
        /// <param name="maxDaysOld">How far back to go in time</param>
        /// <returns></returns>
        public static async Task<List<ErrorLogEntry>> QueryAsync(Context ctxt, int maxDaysOld)
        {
            var output = new List<ErrorLogEntry>();
            string sql = "SELECT id, created FROM errorlog WHERE created > @since ORDER BY id DESC";
            var select = Sql.Parse(sql);
            select.AddParam("@since", DateTime.UtcNow - TimeSpan.FromDays(maxDaysOld));
            metastrings.ListDictionary<long, DateTime> itemIds = 
                await ctxt.ExecDictAsync<long, DateTime>(select).ConfigureAwait(false);
            foreach (var kvp in itemIds.Entries)
            {
                long itemId = kvp.Key;
                DateTime created = kvp.Value;

                string logMessage = 
                    await LongStrings.GetStringAsync(ctxt, itemId, "msg").ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(logMessage))
                    continue;

                var newEntry = new ErrorLogEntry()
                {
                    when = created,
                    msg = logMessage
                };
                output.Add(newEntry);
            }
            return output;
        }

        /// <summary>
        /// All messages from the metastrings errorlogs table
        /// </summary>
        /// <param name="ctxt">Database connection</param>
        /// <returns></returns>
        public static async Task ClearAsync(Context ctxt)
        {
            await ctxt.Cmd.DropAsync("errorlog").ConfigureAwait(false);
        }

        private static string TrimErrorMsg(string msg)
        {
            if (msg.Length <= cMaxErrorgMsgLen)
                return msg;
            else
                return msg.Substring(0, cMaxErrorgMsgLen - "...".Length) + "...";
        }

        private const int cMaxErrorgMsgLen = 64 * 1024 - 1;
    }
}

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using metastrings;

namespace metascript
{
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
        /// <param name="likePattern">Log messages query</param>
        /// <param name="maxDaysOld">How far back to go in time</param>
        /// <returns></returns>
        public static async Task<List<ErrorLogEntry>> QueryAsync(Context ctxt, string likePattern, int maxDaysOld)
        {
            var output = new List<ErrorLogEntry>();
            string sql = "SELECT id, created FROM errorlog WHERE created > @since ORDER BY created DESC";
            var select = Sql.Parse(sql);
            select.AddParam("@since", DateTime.UtcNow - TimeSpan.FromDays(maxDaysOld));
            List<long> itemIds = await ctxt.ExecListAsync<long>(select).ConfigureAwait(false);
            foreach (long itemId in itemIds)
            {
                string logMessage = 
                    await LongStrings.GetStringAsync(ctxt, itemId, "msg", likePattern).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(logMessage))
                    continue;

                var entrySelect = Sql.Parse("SELECT created FROM errorlog WHERE id = @id");
                entrySelect.AddParam("@id", itemId);
                using (var reader = await ctxt.ExecSelectAsync(entrySelect).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var newEntry = new ErrorLogEntry()
                        {
                            when = reader.GetDateTime(0),
                            msg = logMessage
                        };
                        output.Add(newEntry);
                    }
                }
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

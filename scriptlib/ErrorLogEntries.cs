using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using metastrings;

namespace metascript
{
    /// <summary>
    /// Query for log entries.
    /// </summary>
    public class LogQuery
    {
        public int maxAgeDays { get; set; }
        public int maxResults { get; set; }
    }

    /// <summary>
    /// Response to log query.
    /// </summary>
    public class LogEntry
    {
        public string timestamp { get; set; }
        public string msg { get; set; }
    }

    /// <summary>
    /// Class for getting error log entries.
    /// </summary>
    public static class ErrorLogEntries
    {
        /// <summary>
        /// Issue a query to get error logs.
        /// </summary>
        public static async Task<List<LogEntry>> GetErrorLogsAsync(Context ctxt, LogQuery logQuery)
        {
            List<LogEntry> output = new List<LogEntry>();
            {
                var errorEntries = await ErrorLog.QueryAsync(ctxt, logQuery.maxAgeDays).ConfigureAwait(false);
                foreach (var entry in errorEntries)
                {
                    output.Add
                    (
                        new LogEntry()
                        {
                            timestamp = entry.when.ToString("o"),
                            msg = entry.msg
                        }
                    );
                }
            }

            return output;
        }
    }
}

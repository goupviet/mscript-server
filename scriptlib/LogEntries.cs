using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using metastrings;

namespace metascript
{
    public class LogQuery
    {
        public int maxAgeDays { get; set; }
        public string like { get; set; }
        public int maxResults { get; set; }
    }

    public class LogEntry
    {
        public string timestamp { get; set; }
        public string msg { get; set; }
    }

    public static class LogEntries
    {
        public static async Task<List<LogEntry>> GetLogEntriesAsync(Context ctxt, LogQuery logQuery)
        {
            List<LogEntry> output = new List<LogEntry>();
            {
                var errorEntries = await ErrorLog.QueryAsync(ctxt, logQuery.like, logQuery.maxAgeDays).ConfigureAwait(false);
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

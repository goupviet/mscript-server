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
        public int level { get; set; }
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
                            level = (int)LogLevel.ERROR,
                            timestamp = entry.when.ToString("o"),
                            msg = entry.msg
                        }
                    );
                }
            }

            {
                Dictionary<string, object> cmdParams = new Dictionary<string, object>();

                string sql = "SELECT logdate, loglevel, msg FROM userlogs";

                sql += $"\nWHERE created > @logDate";
                cmdParams.Add("@logDate", DateTime.UtcNow - TimeSpan.FromDays(logQuery.maxAgeDays));

                if (!string.IsNullOrWhiteSpace(logQuery.like))
                {
                    sql += $"\nAND msg LIKE @like";
                    cmdParams.Add("@like", logQuery.like);
                }

                sql += "\nORDER BY logdate DESC";
                sql += "\nLIMIT " + logQuery.maxResults;

                var select = Sql.Parse(sql);
                select.cmdParams = cmdParams;

                using (var reader = await ctxt.ExecSelectAsync(select).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var newEntry =
                            new LogEntry()
                            {
                                timestamp = reader.GetString(0),
                                level = (int)reader.GetDouble(1),
                                msg = reader.GetString(2)
                            };
                        output.Add(newEntry);
                    }
                }
            }

            output.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            return output;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using metastrings;

namespace metascript
{
    public class LogQuery
    {
        public long userId { get; set; } = -1;
        public string ip { get; set; }
        public int maxAgeDays { get; set; }
        public string like { get; set; }
        public int maxResults { get; set; }
    }

    public class LogEntry
    {
        public string timestamp { get; set; }
        public int level { get; set; }
        public long userId { get; set; }
        public string ip { get; set; }
        public string msg { get; set; }
    }

    public static class LogEntries
    {
        public static async Task<List<LogEntry>> GetLogEntriesAsync(Context ctxt, LogQuery logQuery)
        {
            List<LogEntry> output = new List<LogEntry>();
            {
                var errorEntries = await ErrorLog.QueryAsync(ctxt, logQuery.like, logQuery.maxAgeDays);
                foreach (var entry in errorEntries)
                {
                    if (!string.IsNullOrWhiteSpace(logQuery.ip) && entry.ip != logQuery.ip)
                        continue;

                    if (logQuery.userId >= 0 && entry.userId != logQuery.userId)
                        continue;

                    output.Add
                    (
                        new LogEntry()
                        {
                            ip = entry.ip,
                            level = (int)LogLevel.ERROR,
                            timestamp = entry.when.ToString("o"),
                            userId = entry.userId,
                            msg = entry.msg
                        }
                    );
                }
            }

            {
                Dictionary<string, object> cmdParams = new Dictionary<string, object>();

                string sql = "SELECT logdate, loglevel, userid, ip, msg FROM userlogs";

                sql += $"\nWHERE created > @logDate";
                cmdParams.Add("@logDate", DateTime.UtcNow - TimeSpan.FromDays(logQuery.maxAgeDays));

                if (logQuery.userId >= 0)
                {
                    sql += $"\nAND userid = @userid";
                    cmdParams.Add("@userid", logQuery.userId);
                }

                if (!string.IsNullOrWhiteSpace(logQuery.ip))
                {
                    sql += $"\nAND ip = @ip";
                    cmdParams.Add("@ip", logQuery.ip);
                }

                if (!string.IsNullOrWhiteSpace(logQuery.like))
                {
                    sql += $"\nAND msg LIKE @like";
                    cmdParams.Add("@like", logQuery.like);
                }

                sql += "\nORDER BY logdate DESC";
                sql += "\nLIMIT " + logQuery.maxResults;

                var select = Sql.Parse(sql);
                select.cmdParams = cmdParams;

                using (var reader = await ctxt.ExecSelectAsync(select))
                {
                    while (await reader.ReadAsync())
                    {
                        var newEntry =
                            new LogEntry()
                            {
                                timestamp = reader.GetString(0),
                                level = (int)reader.GetDouble(1),
                                userId = (long)reader.GetDouble(2),
                                ip = reader.GetString(3),
                                msg = reader.GetString(4)
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

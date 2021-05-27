using System;
using System.Threading.Tasks;
using System.Text;

namespace metascript
{
    /// <summary>
    /// Handler to get error log entries.
    /// </summary>
    class GetErrorLog : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            int num = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var logEntry in await ErrorLog.QueryAsync(state.MsCtxt, 30).ConfigureAwait(false))
            {
                ++num;
                sb.AppendLine($"{num}. {logEntry.when} - {logEntry.msg}");
            }
            string text = sb.ToString();
            await state.WriteResponseAsync(text).ConfigureAwait(false);
        }
    }
}

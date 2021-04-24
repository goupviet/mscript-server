using System;
using System.Threading.Tasks;

using metastrings;
namespace metascript
{
    public class Session
    {
        // NOTE: Do not add any logging calls to GetSessions or you'll stack overflow!
        public static async Task<long> GetSessionAsync(Context ctxt, string cookieValue)
        {
            var select = Sql.Parse("SELECT userid FROM sessions WHERE value = @cookie");
            select.AddParam("@cookie", cookieValue);
            using (var reader = await ctxt.ExecSelectAsync(select))
            {
                if (!await reader.ReadAsync())
                    return -1;

                long userId = (long)reader.GetDouble(0);
                return userId;
            }
        }

        public static async Task<string> CreateSessionAsync(HttpState state, long userId)
        {
            string cookie = MUtils.CreateToken(64);
            await WebUtils.LogTraceAsync(state, "CreateSession: {0}: {1}", userId, cookie);
            var define = new Define("sessions", cookie);
            define.Set("userid", userId);
            await state.MsCtxt.Cmd.DefineAsync(define);
            return cookie;
        }

        public static async Task DeleteSessionAsync(HttpState state, string cookieValue)
        {
            await WebUtils.LogTraceAsync(state, "DeleteSession: {0}", cookieValue);
            await state.MsCtxt.Cmd.DeleteAsync(new Delete("sessions", cookieValue));
        }

        public static async Task ForceUserOutAsync(HttpState state, long userId)
        {
            await WebUtils.LogInfoAsync(state, $"ForceUserOut: {userId}");
            var select = Sql.Parse("SELECT value FROM sessions WHERE userid = @userid");
            select.AddParam("@userid", userId);
            var sessionValues = await state.MsCtxt.ExecListAsync<object>(select);
            await state.MsCtxt.Cmd.DeleteAsync("sessions", sessionValues);
        }

        public static async Task ResetAsync(Context ctxt)
        {
            await ctxt.Cmd.DropAsync("sessions");
        }
    }
}

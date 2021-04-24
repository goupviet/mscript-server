using System;
using System.Threading.Tasks;

namespace metascript
{
    class Logout : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            var sessionKey = WebUtils.GetField("session", state.RequestCookies);
            if (!string.IsNullOrWhiteSpace(sessionKey))
            {
                await WebUtils.LogInfoAsync(state, $"Logout: {sessionKey}");
                await Session.DeleteSessionAsync(state, sessionKey);
            }
            else
                await WebUtils.LogTraceAsync(state, "Logout: (missing cookie)");

            WebUtils.ClearUserSession(state);
            await state.FinishWithMessageAsync("index.html", "You are now signed out");
        }
    }
}

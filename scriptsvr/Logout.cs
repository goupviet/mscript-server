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
                await WebUtils.LogInfoAsync(state, $"Logout: {sessionKey}").ConfigureAwait(false);
                await Session.DeleteSessionAsync(state, sessionKey).ConfigureAwait(false);
            }
            else
                await WebUtils.LogTraceAsync(state, "Logout: (missing cookie)").ConfigureAwait(false);

            WebUtils.ClearUserSession(state);
            await state.FinishWithMessageAsync("index.html", "You are now signed out").ConfigureAwait(false);
        }
    }
}

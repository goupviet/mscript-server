using System;
using System.Threading.Tasks;

namespace metascript
{
    class GetScriptText : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            state.UserId = await WebUtils.GetLoggedInUserIdAsync(state).ConfigureAwait(false);
            if (state.UserId < 0)
                throw new UserException("Sorry, you need to be logged in to get script text");

            string scriptName = state.HttpCtxt.Request.QueryString["name"];
            if (string.IsNullOrWhiteSpace(scriptName))
                throw new UserException("Specify the script you want to get");

            string scriptText = await Script.GetScriptTextAsync(state, state.UserId, scriptName).ConfigureAwait(false);
            await WebUtils.LogTraceAsync(state, "GetScriptText: len = {0}", scriptText.Length).ConfigureAwait(false);
            if (string.IsNullOrEmpty(scriptText))
                return;

            await state.WriteResponseAsync(scriptText).ConfigureAwait(false);
        }
    }
}

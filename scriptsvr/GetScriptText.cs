using System;
using System.Threading.Tasks;

namespace metascript
{
    /// <summary>
    /// Handler to get the text of a script.
    /// </summary>
    class GetScriptText : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            string scriptName = state.HttpCtxt.Request.QueryString["name"];
            if (string.IsNullOrWhiteSpace(scriptName))
                throw new UserException("Specify the script you want to get");

            string scriptText = await Script.GetScriptTextAsync(state, scriptName).ConfigureAwait(false);
            if (string.IsNullOrEmpty(scriptText))
                return;

            await state.WriteResponseAsync(scriptText).ConfigureAwait(false);
        }
    }
}

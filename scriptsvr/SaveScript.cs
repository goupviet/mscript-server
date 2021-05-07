using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace metascript
{
    class SaveScript : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            string scriptName = state.HttpCtxt.Request.QueryString["name"];
            if (string.IsNullOrWhiteSpace(scriptName))
                throw new UserException("Specify the script you want to save");

            string scriptText = await state.GetRequestPostAsync().ConfigureAwait(false);

            await Script.SaveScriptAsync
            (
                state, 
                new Script() 
                { 
                    name = scriptName, 
                    text = scriptText
                }
            ).ConfigureAwait(false);
        }
    }
}

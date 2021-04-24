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
            state.UserId = await WebUtils.GetLoggedInUserIdAsync(state);
            if (state.UserId < 0)
                throw new UserException("Sorry, you need to be logged in to save scripts");

            string scriptName = state.HttpCtxt.Request.QueryString["name"];
            if (string.IsNullOrWhiteSpace(scriptName))
                throw new UserException("Specify the script you want to save");

            string scriptText = await state.GetRequestPostAsync();

            await Script.SaveScriptAsync
            (
                state, 
                new Script() 
                { 
                    userId = state.UserId,
                    name = scriptName, 
                    text = scriptText
                }
            );
        }
    }
}

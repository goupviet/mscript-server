using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace metascript
{
    class GetScriptNames : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            state.UserId = await WebUtils.GetLoggedInUserIdAsync(state).ConfigureAwait(false);
            if (state.UserId < 0)
                throw new UserException("Sorry, you need to be logged in to get scripts");

            List<string> scriptNames = await Script.GetUserScriptNamesAsync(state, state.UserId).ConfigureAwait(false);
            await state.WriteResponseAsync(JsonConvert.SerializeObject(scriptNames)).ConfigureAwait(false);
        }
    }
}

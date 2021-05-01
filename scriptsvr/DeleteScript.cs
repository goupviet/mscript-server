using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace metascript
{
    class DeleteScript : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            state.UserId = await WebUtils.GetLoggedInUserIdAsync(state).ConfigureAwait(false);
            if (state.UserId < 0)
                throw new UserException("Sorry, you need to be logged in to delete scripts");

            string name = state.HttpCtxt.Request.QueryString["name"];
            await Script.DeleteScriptAsync(state, state.UserId, name).ConfigureAwait(false);
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace metascript
{
    class RenameScript : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            state.UserId = await WebUtils.GetLoggedInUserIdAsync(state);
            if (state.UserId < 0)
                throw new UserException("Sorry, you need to be logged in to rename scripts");

            string oldName = state.HttpCtxt.Request.QueryString["oldName"];
            string newName = state.HttpCtxt.Request.QueryString["newName"];

            await Script.RenameScriptAsync(state, state.UserId, oldName, newName);
        }
    }
}

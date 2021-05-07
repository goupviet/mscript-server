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
            string oldName = state.HttpCtxt.Request.QueryString["oldName"];
            string newName = state.HttpCtxt.Request.QueryString["newName"];
            await Script.RenameScriptAsync(state, oldName, newName).ConfigureAwait(false);
        }
    }
}

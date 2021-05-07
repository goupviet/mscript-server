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
            string name = state.HttpCtxt.Request.QueryString["name"];
            await Script.DeleteScriptAsync(state, name).ConfigureAwait(false);
        }
    }
}

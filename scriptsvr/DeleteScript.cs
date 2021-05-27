using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace metascript
{
    /// <summary>
    /// Handler to delete a script.
    /// </summary>
    class DeleteScript : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            string name = state.HttpCtxt.Request.QueryString["name"];
            await Script.DeleteScriptAsync(state, name).ConfigureAwait(false);
        }
    }
}

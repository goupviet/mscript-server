using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace metascript
{
    /// <summary>
    /// Handler to get the names of the user's scripts.
    /// </summary>
    class GetScriptNames : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            List<string> scriptNames = await Script.GetScriptNamesAsync(state).ConfigureAwait(false);
            await state.WriteResponseAsync(JsonConvert.SerializeObject(scriptNames)).ConfigureAwait(false);
        }
    }
}

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
            List<string> scriptNames = await Script.GetScriptNamesAsync(state).ConfigureAwait(false);
            await state.WriteResponseAsync(JsonConvert.SerializeObject(scriptNames)).ConfigureAwait(false);
        }
    }
}

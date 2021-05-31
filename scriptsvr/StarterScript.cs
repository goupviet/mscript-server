using System;
using System.Threading.Tasks;

namespace metascript
{
    /// <summary>
    /// Handler for clients to start out with.
    /// </summary>
    class StarterScript : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            await state.WriteResponseAsync("The formatted web page will appear here").ConfigureAwait(false);
        }
    }
}

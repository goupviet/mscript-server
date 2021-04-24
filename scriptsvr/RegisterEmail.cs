using System;
using System.Threading.Tasks;
using System.IO;

namespace metascript
{
    class RegisterEmail : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            string email = state.HttpCtxt.Request.QueryString["email"];
            string token = await EmailTokens.CreateEmailTokenAsync(state.MsCtxt, email);
            await WebUtils.LogTraceAsync(state, "RegisterEmail: {0} - {1}", email, token);
            await state.WriteResponseAsync(token);
        }
    }
}

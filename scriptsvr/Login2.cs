using System;
using System.Threading.Tasks;
using System.Net;

namespace metascript
{
    class Login2 : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            var requestFieldsDict = await state.GetRequestFieldsAsync();
            string email = WebUtils.GetField("email", requestFieldsDict);
            string loginToken = WebUtils.GetField("token", requestFieldsDict);
            await WebUtils.OnLoginLinkAsync(state, loginToken, email);
        }
    }
}

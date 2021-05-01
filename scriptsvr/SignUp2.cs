using System;
using System.Threading.Tasks;
using System.Net;

namespace metascript
{
    class SignUp2 : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            state.ReturnPage = "signup.html";

            var requestFieldsDict = await state.GetRequestFieldsAsync().ConfigureAwait(false);

            string email = WebUtils.GetField("email", requestFieldsDict);
            string name = WebUtils.GetField("name", requestFieldsDict);
            string loginToken = WebUtils.GetField("token", requestFieldsDict);

            state.ReturnPage = $"signup.html?email={WebUtility.UrlEncode(email)}&name={WebUtility.UrlEncode(name)}";
            await WebUtils.OnLoginLinkAsync(state, loginToken, email).ConfigureAwait(false);
        }
    }
}

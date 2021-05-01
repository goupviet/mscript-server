using System;
using System.Threading.Tasks;
using System.Net;

namespace metascript
{
    class Login : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            state.ReturnPage = "signin.html";
            var requestFieldsDict = await state.GetRequestFieldsAsync().ConfigureAwait(false);

            string email = WebUtils.GetField("email", requestFieldsDict);
            string signature = WebUtils.GetField("signature", requestFieldsDict);
            await EmailTokens.ValidateRequestAsync(state, email, signature).ConfigureAwait(false);

            await WebUtils.LogInfoAsync(state, $"Login: {email}").ConfigureAwait(false);
            state.ReturnPage = $"signin.html?email={WebUtility.UrlEncode(email)}";

            state.UserId = await User.GetUserIdFromEmailAsync(state.MsCtxt, email).ConfigureAwait(false);
            if (state.UserId < 0)
            {
                await state.FinishWithMessageAsync("signup.html?email=" + WebUtility.UrlEncode(email), $"No engineer found with that email address\nSign up now").ConfigureAwait(false);
                return;
            }

            await WebUtils.LogTraceAsync(state, "Login: userId: {0}", state.UserId);

            User user = await User.GetUserAsync(state.MsCtxt, state.UserId).ConfigureAwait(false);
            await WebUtils.LogTraceAsync(state, "Login: user.Name: {0}", user.Name).ConfigureAwait(false);
            if (user.Blocked)
                throw new UserException("Sorry, this account is closed");

            await User.SetLoginTokenAsync(state, user).ConfigureAwait(false);

            string nextPage = $"signin2.html?email={WebUtility.UrlEncode(user.Email)}";
            if (!Email.WillSendEmail) // for testing
            {
                nextPage += $"&token={WebUtility.UrlEncode(user.LoginToken)}";
                await state.FinishAsync(nextPage).ConfigureAwait(false);
            }
            else
            {
                await WebUtils.DoLoginLinkAsync(state, nextPage, user, "mscript sign in").ConfigureAwait(false);
            }
        }
    }
}

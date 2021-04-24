using System;
using System.Threading.Tasks;
using System.Net;

namespace metascript
{
    class SignUp : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            state.ReturnPage = "signup.html";
                
            var requestFieldsDict = await state.GetRequestFieldsAsync();

            string name = WebUtils.GetField("name", requestFieldsDict);
            if (string.IsNullOrWhiteSpace(name))
                throw new UserException("Please specify the name you would like to use");

            string email = WebUtils.GetField("email", requestFieldsDict);
            if (string.IsNullOrWhiteSpace(email))
                throw new UserException("Please enter your email address");

            if (!Email.IsEmailValid(email))
                throw new UserException($"Sorry, '{email}' is not a valid email address");

            string signature = WebUtils.GetField("signature", requestFieldsDict);
            await EmailTokens.ValidateRequestAsync(state, email, signature);

            if ((await User.GetUserIdFromEmailAsync(state.MsCtxt, email)) >= 0)
            {
                await state.FinishWithMessageAsync("signin.html?email=" + WebUtility.UrlEncode(email), $"{email} already signed up\nSign in now");
                return;
            }

            if ((await User.GetUserIdFromNameAsync(state.MsCtxt, name)) >= 0)
            {
                await state.FinishWithMessageAsync("signin.html?email=" + WebUtility.UrlEncode(email), $"{name} already signed up\nSign in now");
                return;
            }

            var user = await User.CreateUserAsync(state, email, name);
            state.UserId = user.Id;
            await User.SetLoginTokenAsync(state, user);

            string nextPage = $"signup2.html?email={WebUtility.UrlEncode(user.Email)}&name={WebUtility.UrlEncode(name)}";
            if (!Email.WillSendEmail) // for testing
            {
                nextPage += $"&token={WebUtility.UrlEncode(user.LoginToken)}";
                await state.FinishAsync(nextPage);
            }
            else
                await WebUtils.DoLoginLinkAsync(state, nextPage, user, "Welcome to Balogna Beats!");
        }
    }
}

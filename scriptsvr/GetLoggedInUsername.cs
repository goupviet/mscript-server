using System;
using System.Threading.Tasks;
using System.IO;

namespace metascript
{
    class GetLoggedInUsername : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            state.UserId = await WebUtils.GetLoggedInUserIdAsync(state).ConfigureAwait(false);
            if (state.UserId < 0)
                return;

            User user = await User.GetUserAsync(state.MsCtxt, state.UserId).ConfigureAwait(false);
            if (user.Blocked)
                throw new UserException("Sorry, this account is closed");

            await state.WriteResponseAsync(user.Name).ConfigureAwait(false);
        }
    }
}

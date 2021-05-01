using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using metastrings;

namespace metascript
{
    public class AuthorScript
    {
        public string Author { get; set; }
        public string ScriptName { get; set; }
    }

    public static class WebUtils
    {
        public static void ValidateEmail(string email)
        {
            if (!Email.IsEmailValid(email))
                throw new UserException($"Sorry, '{email}' is not a valid email address");
        }

        public static async Task<long> GetLoggedInUserIdAsync(HttpState state)
        {
            string sessionKey = GetField("session", state.RequestCookies); 
            if (string.IsNullOrWhiteSpace(sessionKey))
                return -1;

            long userId = await Session.GetSessionAsync(state.MsCtxt, sessionKey).ConfigureAwait(false);
            if (userId < 0)
                ClearUserSession(state);
            return userId;
        }

        public static string GetField(string key, Dictionary<string, string> dict)
        {
            string val;
            if (dict == null || !dict.TryGetValue(key, out val))
                val = "";
            return val;
        }

        public static async Task LogErrorAsync(HttpState state, string msg, long userId)
        {
            var logCtxt =
                new LogContext()
                {
                    userId = userId,
                    ip = GetClientIpAddress(state)
                };
            await Logs.LogErrorAsync(state.MsCtxt, msg, logCtxt).ConfigureAwait(false);
        }

        public static async Task LogAsync(HttpState state, LogLevel level, string msg)
        {
            if (Logs.ShouldSkip(level))
                return;

            var logCtxt =
                new LogContext()
                {
                    userId = await GetLoggedInUserIdAsync(state).ConfigureAwait(false),
                    ip = GetClientIpAddress(state)
                };
            await Logs.LogAsync(state.MsCtxt, level, msg, logCtxt).ConfigureAwait(false);
        }

        public static async Task LogInfoAsync(HttpState state, string msg)
        {
            await LogAsync(state, LogLevel.INFO, msg).ConfigureAwait(false);
        }

        public static async Task LogTraceAsync(HttpState state, string msg)
        {
            if (Logs.ShouldSkip(LogLevel.TRACE))
                return;
            await LogAsync(state, LogLevel.TRACE, msg).ConfigureAwait(false);
        }

        public static async Task LogTraceAsync(HttpState state, string msg, object arg0)
        {
            if (Logs.ShouldSkip(LogLevel.TRACE))
                return;

            msg = string.Format(msg, arg0);
            await LogAsync(state, LogLevel.TRACE, msg).ConfigureAwait(false);
        }

        public static async Task LogTraceAsync(HttpState state, string msg, object arg0, object arg1)
        {
            if (Logs.ShouldSkip(LogLevel.TRACE))
                return;

            msg = string.Format(msg, arg0, arg1);
            await LogAsync(state, LogLevel.TRACE, msg).ConfigureAwait(false);
        }

        public static async Task LogTraceAsync(HttpState state, string msg, params object [] args)
        {
            if (Logs.ShouldSkip(LogLevel.TRACE))
                return;

            msg = string.Format(msg, args);
            await LogAsync(state, LogLevel.TRACE, msg).ConfigureAwait(false);
        }

        public static async Task<long> EnsureLoggedInAsync(HttpState state, string area)
        {
            long userId = await GetLoggedInUserIdAsync(state).ConfigureAwait(false);
            if (userId < 0)
            {
                await LogInfoAsync(state, $"{area}: Not logged in").ConfigureAwait(false);
                await state.SetFinalStatusAsync(401, "Sorry, you are not logged in").ConfigureAwait(false);
            }
            return userId;
        }
        
        public static async Task OnLoginLinkAsync(HttpState state, string token, string email)
        {
            try
            {
                ValidateEmail(email);
                if (string.IsNullOrWhiteSpace(token))
                    throw new UserException("Your login token is missing, try again");

                await LogTraceAsync(state, "Login Link: {0} - {1}", token, email).ConfigureAwait(false);
                User user = await User.HandleLoginTokenAsync(state, token, email).ConfigureAwait(false);
                await CreateUserSessionAsync(state, user).ConfigureAwait(false);
                await state.FinishWithMessageAsync("index.html", "You are now signed in").ConfigureAwait(false);
                return;
            }
            catch
            {
                // Don't care too much
            }

            ClearUserSession(state);
            await state.FinishWithMessageAsync("index.html", "Sorry, your login attempt failed, please try again").ConfigureAwait(false);
        }

        public static async Task CreateUserSessionAsync(HttpState state, User user)
        {
            string sessionToken = await Session.CreateSessionAsync(state, user.Id).ConfigureAwait(false);
            state.SetResponseSession(sessionToken);
        }

        public static void ClearUserSession(HttpState state)
        {
            state.SetResponseSession("");
        }

        public static async Task DoLoginLinkAsync(HttpState state, string page, User user, string subject)
        {
            ValidateEmail(user.Email);

            var emailTemplates = EmailTemplates.Templates;
            string textTemplate = emailTemplates.Item1.Replace("[ACCESS CODE]", user.LoginToken).Replace("[NAME]", user.Name);
            string htmlTemplate = emailTemplates.Item2.Replace("[ACCESS CODE]", user.LoginToken).Replace("[NAME]", user.Name);

            await Email.SendEmailAsync(state, user.Email, user.Name, subject, textTemplate, htmlTemplate).ConfigureAwait(false);

            await state.FinishWithMessageAsync(page, "Check your email for your sign in token").ConfigureAwait(false);
        }

        public static string GetClientIpAddress(HttpState state)
        {
            if (state.HttpCtxt == null)
                return "0.0.0.0";

            var request = state.HttpCtxt.Request;
            var forwardedIp = request.Headers["X-Forwarded-For"]; // lb
            if (!string.IsNullOrWhiteSpace(forwardedIp))
                return forwardedIp;
            else
                return request.UserHostAddress;
        }

        public static async Task HandleUserErrorAsync(HttpState state, UserException exp)
        {
            var logCtxt = new LogContext() { ip = GetClientIpAddress(state), userId = state.UserId };
            string errorInfo = Errors.GetErrorInfo(state, GetClientIpAddress(state));
            await Logs.LogErrorAsync(state.MsCtxt, $"User Exception: {errorInfo}: {exp.Message}", logCtxt).ConfigureAwait(false);
            await state.SetFinalStatusAsync(400, exp.Message).ConfigureAwait(false);
        }

        public static async Task<AuthorScript> GetAuthorScript(HttpState state)
        {
            string author = state.HttpCtxt.Request.QueryString["author"];
            if (string.IsNullOrWhiteSpace(author))
            {
                await state.SetFinalStatusAsync(403, "Sorry, you have to specify the author").ConfigureAwait(false);
                return null;
            }
            author = author.Replace('+', ' ');

            string scriptName = state.HttpCtxt.Request.QueryString["scriptName"];
            if (string.IsNullOrWhiteSpace(scriptName))
            {
                await state.SetFinalStatusAsync(403, "Sorry, you have to specify the script name").ConfigureAwait(false);
                return null;
            }
            scriptName = scriptName.Replace('+', ' ');

            return new AuthorScript() { Author = author, ScriptName = scriptName };
        }
    }
}

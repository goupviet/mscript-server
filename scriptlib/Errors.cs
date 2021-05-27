using System;
using System.Threading.Tasks;
using System.Linq;

namespace metascript
{
    /// <summary>
    /// Exception class for messages to deliver to the user.
    /// </summary>
    public class UserException : Exception
    {
        public UserException(string msg) : base(msg) { }
    }

    /// <summary>
    /// Internal exception type.
    /// </summary>
    public class MException : Exception
    {
        public MException(string msg) : base(msg) { }
    }

    public static class Errors
    {
        /// <summary>
        /// Global error handler that logs errors and finishes pages.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static async Task HandleErrorAsync(HttpState state, Exception exp)
        {
            while (exp.InnerException != null)
                exp = exp.InnerException;

            Console.WriteLine($"ERROR: {exp.GetType().FullName}: {exp.Message}");
            
            string errorInfo = GetErrorInfo(state);
            if (exp is UserException)
            {
                await ErrorLog.LogAsync(state.MsCtxt, $"User Exception: {errorInfo}: {exp.Message}").ConfigureAwait(false);
                await state.FinishWithMessageAsync(exp.Message).ConfigureAwait(false);
            }
            else
            {
                await ErrorLog.LogAsync(state.MsCtxt, $"EXCEPTION: {errorInfo}: {exp}").ConfigureAwait(false);
                await state.FinishWithMessageAsync("Sorry, an unexpected error occurred.\n\nTry again later.  Failing that, email contact@mscript.info for help").ConfigureAwait(false);
            }
        }

       private static string GetErrorInfo(HttpState state)
       {
            string errorInfo =
                state.HttpCtxt == null
                ? "non-HTTP" 
                : $"{state.HttpCtxt.Request.Url.Segments.Last()}{state.HttpCtxt.Request.Url.Query}";
            return errorInfo;
        }
    }
}

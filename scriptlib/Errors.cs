using System;
using System.Threading.Tasks;
using System.Linq;

namespace metascript
{
    public class UserException : Exception
    {
        public UserException(string msg) : base(msg) { }
    }

    public class MException : Exception
    {
        public MException(string msg) : base(msg) { }
    }

    public static class Errors
    {
        public static async Task HandleErrorAsync(HttpState state, Exception exp)
        {
            while (exp.InnerException != null)
                exp = exp.InnerException;
#if DEBUG
            Console.WriteLine($"ERROR: " + exp);
#endif
            string errorInfo = GetErrorInfo(state);
            if (!string.IsNullOrWhiteSpace(state.ReturnPage) && exp is UserException)
            {
                await Logs.LogErrorAsync(state.MsCtxt, $"User Exception: {errorInfo}: {exp.Message}").ConfigureAwait(false);
                await state.FinishWithMessageAsync(state.ReturnPage, exp.Message).ConfigureAwait(false);
            }
            else
            {
                await Logs.LogErrorAsync(state.MsCtxt, $"EXCEPTION: {errorInfo}: {exp}").ConfigureAwait(false);
                await state.FinishWithMessageAsync(state.ReturnPage, "Sorry, an unexpected error occurred.\n\nTry again later.  Failing that, email contact@mscript.info for help").ConfigureAwait(false);
            }
        }

       public static string GetErrorInfo(HttpState state)
       {
            string errorInfo =
                state.HttpCtxt == null
                ? "non-HTTP" 
                : $"{state.HttpCtxt.Request.Url.Segments.Last()}{state.HttpCtxt.Request.Url.Query}";
            return errorInfo;
        }
    }
}

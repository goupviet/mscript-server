﻿using System;
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
            string userIp = WebUtils.GetClientIpAddress(state);
            LogContext logCtxt = new LogContext() { ip = userIp, userId = state.UserId };
            string errorInfo = GetErrorInfo(state, userIp);
            if (!string.IsNullOrWhiteSpace(state.ReturnPage) && exp is UserException)
            {
                await Logs.LogErrorAsync(state.MsCtxt, $"User Exception: {errorInfo}: {exp.Message}", logCtxt);
                await state.FinishWithMessageAsync(state.ReturnPage, exp.Message);
            }
            else
            {
                await Logs.LogErrorAsync(state.MsCtxt, $"EXCEPTION: {errorInfo}: {exp}", logCtxt);
                await state.FinishWithMessageAsync(state.ReturnPage, "Sorry, an unexpected error occurred.\n\nTry again later, failing that, email contact@mscript.info for help");
            }
        }

       public static string GetErrorInfo(HttpState state, string userIp)
       {
            string errorInfo =
                state.HttpCtxt == null
                ? "non-HTTP" 
                : $"{state.HttpCtxt.Request.Url.Segments.Last()}{state.HttpCtxt.Request.Url.Query}";
            errorInfo += $": {userIp}: {state.UserId}";
            return errorInfo;
        }
    }
}

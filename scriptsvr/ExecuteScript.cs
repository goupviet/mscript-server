using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace metascript
{
    class ExecuteScript : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            state.UserId = await WebUtils.GetLoggedInUserIdAsync(state);
            if (state.UserId < 0)
                throw new UserException("Sorry, you need to be logged in to execute scripts");

            string scriptName = state.HttpCtxt.Request.QueryString["name"];
            if (string.IsNullOrWhiteSpace(scriptName))
                throw new UserException("Specify the script you want to run");
            
            await WebUtils.LogTraceAsync(state, "ExecuteScript: {0}", scriptName);
            var scriptText = await Script.GetScriptTextAsync(state, state.UserId, scriptName);
            if (scriptText == null) // empty is okay
                throw new UserException("Sorry, the script was not found");

            var symbols = new SymbolTable();
            using
            (
                var processor =
                    new ScriptProcessor
                    (
                        scriptText,
                        symbols,
                        state.HttpCtxt.Response.OutputStream,
                        state,
                        sm_scriptFunctions
                    )
                )
            {
                ScriptException collectedExp;
                try
                {
                    await processor.ProcessAsync();
                    return;
                }
                catch (ScriptException exp)
                {
                    collectedExp = exp;
                }
                using (var stream = new StreamWriter(state.HttpCtxt.Response.OutputStream, leaveOpen: true))
                {
                    await stream.WriteAsync
                    (
                        $"\nERROR: {collectedExp.Message}\n" +
                        $"Line {collectedExp.LineNumber}:\n" +
                        $"{collectedExp.Line}\n"
                    );
                }
            }
        }

        private static readonly Dictionary<string, IScriptContextFunction> sm_scriptFunctions =
            ScriptFunctions.GetScriptFunctions();
    }
}

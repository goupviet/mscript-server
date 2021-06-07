using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace metascript
{
    /// <summary>
    /// Handler to execute a script.
    /// </summary>
    class ExecuteScript : IPage
    {
        public async Task HandleRequestAsync(HttpState state)
        {
            string scriptName = state.HttpCtxt.Request.QueryString["script"];
            if (string.IsNullOrWhiteSpace(scriptName))
                throw new UserException("Specify the script you want to run");
            
            var scriptText = await Script.GetScriptTextAsync(state, scriptName).ConfigureAwait(false);
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
                        scriptName,
                        "execute",
                        state,
                        sm_scriptFunctions
                    )
                )
            {
                ScriptException collectedExp;
                try
                {
                    await processor.ProcessAsync().ConfigureAwait(false);
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
                        $"\n" +
                        $"ERROR: {collectedExp.Message}\n" +
                        $"Line {collectedExp.LineNumber}\n" +
                        $"{collectedExp.Line}"
                    ).ConfigureAwait(false);
                }
            }
        }

        private static readonly Dictionary<string, IScriptContextFunction> sm_scriptFunctions =
            ScriptFunctions.GetScriptFunctions();
    }
}

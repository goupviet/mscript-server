using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

namespace metascript
{
    class ScriptFunctions
    {
        public static Dictionary<string, IScriptContextFunction> GetScriptFunctions()
        {
            var retVal = new Dictionary<string, IScriptContextFunction>();
            retVal.Add("input", new InputGetterScriptContextFunction());

            foreach (var kvp in DbScripting.DbFunctions)
                retVal.Add(kvp.Key, kvp.Value);

            return retVal;
        }
    }

    public class InputGetterScriptContextFunction : IScriptContextFunction
    {
        public string Name => "input";
        public List<string> ParamNames => new List<string>() { "name" };
        public Task<object> CallAsync(object context, List<object> paramList)
        {
            HttpListenerContext httpCtxt = (HttpListenerContext)context;
            string key = paramList[0].ToString();
            return Task.FromResult<object>(httpCtxt.Request.QueryString[key]);
        }
    }
}

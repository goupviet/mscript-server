using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using metastrings;

namespace metascript
{
    public class Script
    {
        public long id { get; set; }
        public long userId { get; set; }
        public string name { get; set; }
        public string text { get; set; }

        public static async Task<Script> GetScriptAsync(Context ctxt, long scriptId)
        {
            var select =
                Sql.Parse
                (
                    $"SELECT userid, name FROM scripts WHERE id = @scriptId"
                );
            select.cmdParams = new Dictionary<string, object> { { "@scriptId", scriptId } };
            Script script;
            using (var reader = await ctxt.ExecSelectAsync(select).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync().ConfigureAwait(false))
                    throw new MException("Script not found: " + scriptId);

                script =
                    new Script()
                    {
                        id = scriptId,
                        userId = reader.GetInt64(0),
                        name = reader.GetString(1)
                    };
            }

            script.text =
                await ctxt.Cmd.GetLongStringAsync
                (
                    new LongStringOp()
                    {
                        table = "scripts",
                        fieldName = "text",
                        itemId = scriptId
                    }
                ).ConfigureAwait(false);

            return script;
        }

        public static async Task<List<string>> GetUserScriptNamesAsync(HttpState state, long userId)
        {
            var select =
                Sql.Parse
                (
                    $"SELECT name FROM scripts WHERE userid = @userid"
                );
            select.cmdParams = new Dictionary<string, object> { { "@userId", userId } };
            var scriptNames = await state.MsCtxt.ExecListAsync<string>(select).ConfigureAwait(false);
            return scriptNames;
        }

        public static async Task<string> GetScriptTextAsync(HttpState state, long userId, string name)
        {
            long scriptId;
            {
                var select =
                    Sql.Parse
                    (
                        $"SELECT id FROM scripts WHERE userid = @userid AND name = @name"
                    );
                select.cmdParams = new Dictionary<string, object> { { "@userId", userId }, { "@name", name } };
                scriptId = await state.MsCtxt.ExecScalar64Async(select).ConfigureAwait(false);
                if (scriptId < 0)
                    throw new UserException("Script not found: " + name);
            }

            string text =
                await state.MsCtxt.Cmd.GetLongStringAsync
                (
                    new LongStringOp()
                    {
                        table = "scripts",
                        fieldName = "text",
                        itemId = scriptId
                    }
                ).ConfigureAwait(false);
            return text;
        }

        public static async Task SaveScriptAsync(HttpState state, Script script)
        {
            await WebUtils.LogTraceAsync(state, "SaveScript: {0}", script.name).ConfigureAwait(false);
            string key = $"{script.userId}:{script.name}";
            var define = new Define("scripts", key);
            define.Set("userid", script.userId);
            define.Set("name", script.name);
            await state.MsCtxt.Cmd.DefineAsync(define).ConfigureAwait(false);

            script.id = await state.MsCtxt.GetRowIdAsync("scripts", key).ConfigureAwait(false);
            if (script.text != null)
            {
                await state.MsCtxt.Cmd.PutLongStringAsync
                (
                    new LongStringPut()
                    {
                        table = "scripts",
                        fieldName = "text",
                        itemId = script.id,
                        longString = script.text
                    }
                ).ConfigureAwait(false);
            }
        }

        public static async Task RenameScriptAsync(HttpState state, long userId, string oldName, string newName)
        {
            await WebUtils.LogInfoAsync(state, $"RenameScript: {oldName} -> {newName}").ConfigureAwait(false);
            if (oldName == newName)
                throw new UserException("You cannot set a script title to its current value.");

            long rowId = await state.MsCtxt.GetRowIdAsync("scripts", $"{userId}:{oldName}").ConfigureAwait(false);
            var script = await GetScriptAsync(state.MsCtxt, rowId).ConfigureAwait(false);
            if (script == null)
                throw new UserException("Script to rename not found: " + oldName);

            script.name = newName;
            await SaveScriptAsync(state, script).ConfigureAwait(false);

            await DeleteScriptAsync(state, userId, oldName).ConfigureAwait(false);
        }

        public static async Task DeleteScriptAsync(HttpState state, long userId, string name)
        {
            await WebUtils.LogInfoAsync(state, $"DeleteScript: {name}").ConfigureAwait(false);
            string key = $"{userId}:{name}";
            await state.MsCtxt.Cmd.DeleteAsync("scripts", key).ConfigureAwait(false);
        }

        public static async Task<long> GetScriptIdAsync(Context ctxt, string author, string name)
        {
            long userId;
            {
                var select = Sql.Parse("SELECT id FROM users WHERE name = @author");
                select.AddParam("@author", author);
                userId = await ctxt.ExecScalar64Async(select).ConfigureAwait(false);
                if (userId < 0)
                    return -1;
            }

            long scriptId;
            {
                var select = Sql.Parse("SELECT id FROM scripts WHERE userid = @userId AND name = @name");
                select.AddParam("@userId", userId);
                select.AddParam("@name", name);
                scriptId = await ctxt.ExecScalar64Async(select).ConfigureAwait(false);
            }
            return scriptId;
        }
    }
}

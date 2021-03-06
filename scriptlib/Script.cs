using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using metastrings;

namespace metascript
{
    /// <summary>
    /// Central class for representing a script.
    /// </summary>
    public class Script
    {
        public long id { get; set; }
        public string name { get; set; }
        public string text { get; set; }

        /// <summary>
        /// Given a script ID, get a script, or an exception.
        /// </summary>
        public static async Task<Script> GetScriptAsync(Context ctxt, long scriptId)
        {
            var select = Sql.Parse($"SELECT name FROM scripts WHERE id = @scriptId");
            select.AddParam("@scriptId", scriptId);
            Script script;
            using (var reader = await ctxt.ExecSelectAsync(select).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync().ConfigureAwait(false))
                    throw new MException("Script not found: " + scriptId);

                script =
                    new Script()
                    {
                        id = scriptId,
                        name = reader.GetString(0)
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

        /// <summary>
        /// Get the names of all the user's scripts.
        /// </summary>
        public static async Task<List<string>> GetScriptNamesAsync(HttpState state)
        {
            var select = Sql.Parse($"SELECT name FROM scripts");
            var scriptNames = await state.MsCtxt.ExecListAsync<string>(select).ConfigureAwait(false);
            return scriptNames;
        }

        /// <summary>
        /// Get the text of a script.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<string> GetScriptTextAsync(HttpState state, string name)
        {
            long scriptId;
            {
                var select = Sql.Parse("SELECT id FROM scripts WHERE name = @name");
                select.AddParam("@name", name);
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

        /// <summary>
        /// Save a script to the database.
        /// </summary>
        public static async Task SaveScriptAsync(HttpState state, Script script)
        {
            var define = new Define("scripts", script.name);
            define.Set("name", script.name);
            await state.MsCtxt.Cmd.DefineAsync(define).ConfigureAwait(false);

            script.id = await state.MsCtxt.GetRowIdAsync("scripts", script.name).ConfigureAwait(false);
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

        /// <summary>
        /// Rename a script...carefully.
        /// </summary>
        public static async Task RenameScriptAsync(HttpState state, string oldName, string newName)
        {
            if (oldName == newName)
                throw new UserException("You cannot set a script title to its current value.");

            long rowId = await state.MsCtxt.GetRowIdAsync("scripts", oldName).ConfigureAwait(false);
            var script = await GetScriptAsync(state.MsCtxt, rowId).ConfigureAwait(false);
            if (script == null)
                throw new UserException("Script to rename not found: " + oldName);

            script.name = newName;
            await SaveScriptAsync(state, script).ConfigureAwait(false);

            await DeleteScriptAsync(state, oldName).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete a script.
        /// </summary>
        public static async Task DeleteScriptAsync(HttpState state, string name)
        {
            string key = name;
            await state.MsCtxt.Cmd.DeleteAsync("scripts", key).ConfigureAwait(false);
        }

        /// <summary>
        /// Get the database ID of a script.
        /// </summary>
        public static async Task<long> GetScriptIdAsync(Context ctxt, string name)
        {
            var select = Sql.Parse("SELECT id FROM scripts WHERE name = @name");
            select.AddParam("@name", name);
            long scriptId = await ctxt.ExecScalar64Async(select).ConfigureAwait(false);
            return scriptId;
        }
    }
}

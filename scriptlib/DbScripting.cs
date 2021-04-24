using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using metastrings;

namespace metascript
{
    using list = List<object>;
    using index = ListDictionary<object, object>;

    public class DbFunction : IScriptContextFunction
    {
        public DbFunction(string name, List<string> paramNames)
        {
            m_name = name;
            m_paramNames = paramNames;
        }

        public string Name => m_name;

        public List<string> ParamNames => m_paramNames;

        public async Task<object> CallAsync(object context, list paramList)
        {
            return await DbScripting.DoDbAsync(context, m_name, paramList);
        }

        private string m_name;
        private List<string> m_paramNames;
    }

    public static class DbScripting
    {
        public static readonly Dictionary<string, IScriptContextFunction> DbFunctions =
            new Dictionary<string, IScriptContextFunction>()
            {
                { "msdb.define", new DbFunction("msdb.define", new List<string>() { "table_name", "key_value", "[column_data_index]" })},
                { "msdb.delete", new DbFunction("msdb.delete", new List<string>() { "table_name", "key_value(s)" })},
                { "msdb.drop", new DbFunction("msdb.drop", new List<string>() { "table_name" })},
                { "msdb.selectValue", new DbFunction("msdb.selectValue", new List<string>() { "SQL_query", "[parameters_index]" })},
                { "msdb.selectList", new DbFunction("msdb.selectList", new List<string>() { "SQL_query", "[parameters_index]" })},
                { "msdb.selectIndex", new DbFunction("msdb.selectIndex", new List<string>() { "SQL_query", "[parameters_index]" })},
                { "msdb.selectRecords", new DbFunction("msdb.selectRecords", new List<string>() { "SQL_query", "[parameters_index]" })}
            };

        public static async Task<object> DoDbAsync(object contextObj, string name, list paramList)
        {
            HttpState state = (HttpState)contextObj;
            Context ctxt = state.MsCtxt;

            switch (name)
            {
                case "msdb.define":
                    {
                        if (paramList.Count != 2 && paramList.Count != 3)
                            throw new ScriptException("Incorrect params for define function: table_name, key_value, and optionally a column_data_index");

                        if (!(paramList[0] is string))
                            throw new ScriptException("The first parameter to the define function must be the table name as a string");
                        if (!(paramList[1] is string) && !(paramList[1] is double))
                            throw new ScriptException("The second parameter to the define function must be the key value, either a string or a number");
                        if (paramList.Count >= 3 && !(paramList[2] is index))
                            throw new ScriptException("The third parameter to the define function must be the index of column names and values");

                        string tableName = ScopeTableName(state, (string)paramList[0]);
                        object keyValue = paramList[1];
                        var define = new Define(tableName, keyValue);
                        if (paramList.Count >= 3)
                        {
                            foreach (var kvp in ((index)paramList[2]).Entries)
                            {
                                if (!(kvp.Key is string) || !metastrings.Utils.IsWord((string)kvp.Key) || metastrings.Utils.IsNameReserved((string)kvp.Key))
                                    throw new ScriptException("The key is invalid");
                                if (kvp.Value != null && !(kvp.Value is string) && !(kvp.Value is double))
                                    throw new ScriptException("A value must be either a string or a number, or null");
                                define.Set(Utils.ToString(kvp.Key), kvp.Value);
                            }
                        }
                        await ctxt.Cmd.DefineAsync(define);

                        double rowId = await ctxt.GetRowIdAsync(tableName, keyValue);
                        return rowId;
                    }

                case "msdb.delete":
                    {
                        if (paramList.Count != 2)
                            throw new ScriptException("Incorrect params for delete function: table_name, key_value(s)");
                        if (!(paramList[0] is string))
                            throw new ScriptException("The first parameter to the delete function must be the table name as a string");
                        if (!(paramList[1] is list))
                            throw new ScriptException("The second parameter to the delete function must be list of keys to delete");
                        list valuesToDelete;
                        valuesToDelete = (list)paramList[1];
                        foreach (var val in valuesToDelete)
                        {
                            if (!(val is string) && !(val is double))
                                throw new ScriptException("The the keys of the items to delete must be either strings or numbers");
                        }
                        await ctxt.Cmd.DeleteAsync(ScopeTableName(state, (string)paramList[0]), valuesToDelete);
                        return null;
                    }

                case "msdb.drop":
                    {
                        if (paramList.Count != 1)
                            throw new ScriptException("Incorrect params for drop function: table_name");
                        if (!(paramList[0] is string))
                            throw new ScriptException("The parameter to the drop function must be the table name as a string");
                        await ctxt.Cmd.DropAsync(ScopeTableName(state, (string)paramList[0]));
                        return null;
                    }

                case "msdb.selectValue":
                    {
                        object val = await ctxt.ExecScalarAsync(CreateSelect(state, paramList));
                        val = CoerceDbValue(val);
                        return val;
                    }

                case "msdb.selectList":
                    {
                        list vals = await ctxt.ExecListAsync<object>(CreateSelect(state, paramList));
                        for (int v = 0; v < vals.Count; ++v)
                            vals[v] = CoerceDbValue(vals[v]);
                        return vals;
                    }

                case "msdb.selectIndex":
                    {
                        var  metastringsResult 
                            = await ctxt.ExecDictAsync<object, object>(CreateSelect(state, paramList));
                        index retVal = new index();
                        foreach (var kvp in metastringsResult.Entries)
                            retVal.Add(CoerceDbValue(kvp.Key), CoerceDbValue(kvp.Value));
                        return retVal;
                    }

                case "msdb.selectRecords":
                    {
                        list rows = new list();
                        using (var reader = await ctxt.ExecSelectAsync(CreateSelect(state, paramList)))
                        {
                            while (await reader.ReadAsync())
                            {
                                index row = new index();
                                for (int f = 0; f < reader.FieldCount; ++f)
                                {
                                    object value = reader.GetValue(f);
                                    if (!(value is string))
                                        value = Convert.ToDouble(value);
                                    row.Add(reader.GetName(f), CoerceDbValue(value));
                                }
                                rows.Add(row);
                            }
                        }
                        return rows;
                    }

                default:
                    throw new MException("Unrecognized DB function: " + name);
            }
        }

        private static Select CreateSelect(HttpState state, list paramList)
        {
            if (paramList.Count == 0)
                throw new ScriptException("select functions require a SQL query, and an optional index of parameters to the query");
            else if (paramList.Count > 2)
                throw new ScriptException("select functions take at most a SQL query and an option index of parameters");

            if (!(paramList[0] is string))
                throw new ScriptException("SQL query must be a string");

            var select = Sql.Parse((string)paramList[0]);

            if (paramList.Count > 1)
            {
                select.cmdParams = new Dictionary<string, object>();
                foreach (var kvp in ((index)paramList[1]).Entries)
                {
                    if (!(kvp.Key is string))
                        throw new ScriptException("Invalid query parameter name, must be a string");
                    if (!metastrings.Utils.IsParam((string)kvp.Key))
                        throw new ScriptException("Invalid query parameter name");
                    if (!(kvp.Value is string) && !(kvp.Value is double))
                        throw new ScriptException("Invalid query parameter value, must be a string or a number");
                    select.cmdParams.Add((string)kvp.Key, kvp.Value);
                }
            }

            select.from = ScopeTableName(state, select.from);

            return select;
        }

        private static object CoerceDbValue(object dbValue)
        {
            if (dbValue is double || dbValue is string || dbValue is bool)
                return dbValue;
            if (dbValue is DateTime)
                return ((DateTime)dbValue).ToString("yyyy/MM/dd HH:mm:ss");
            else
                return Convert.ToDouble(dbValue);
        }

        private static string ScopeTableName(HttpState state, string from)
        {
            return $"u{state.UserId}_{from}";
        }
    }
}

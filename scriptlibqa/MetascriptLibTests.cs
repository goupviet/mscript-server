using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace metascript
{
    using list = List<object>;
    using index = ListDictionary<object, object>;

    public class ScriptLib
    {
        [Test]
        public void TestLogs()
        {
            using (var state = new HttpState(null))
            {
                string infoMsg = "Test Info Message " + Guid.NewGuid();
                Logs.LogAsync(state.MsCtxt, LogLevel.INFO, infoMsg).Wait();

                string errorMsg = "Test Error Message " + Guid.NewGuid();
                ErrorLog.LogAsync(state.MsCtxt, errorMsg).Wait();

                Assert.IsFalse(Logs.ShouldSkip(LogLevel.ERROR));

                bool infoFound = false, errorFound = false;
                var logQuery =
                    new LogQuery()
                    {
                        maxAgeDays = 1000,
                        maxResults = 100000,
                        like = "%"
                    };
                var results = LogEntries.GetLogEntriesAsync(state.MsCtxt, logQuery).Result;
                foreach (var result in results)
                {
                    if (result.msg == infoMsg) infoFound = true;
                    if (result.msg == errorMsg) errorFound = true;
                }
                Assert.IsTrue(infoFound);
                Assert.IsTrue(errorFound);
            }
        }

        [Test]
        public void TestScripts()
        {
            using (var state = new HttpState(null))
            {
                Script testScript =
                    new Script()
                    {
                        id = -1,
                        name = "Test Script " + Guid.NewGuid(),
                        text = ">> Hello world!"
                    };
                Script.SaveScriptAsync(state, testScript).Wait();

                Script gottenScript = Script.GetScriptAsync(state.MsCtxt, testScript.id).Result;
                Assert.AreEqual(testScript.name, gottenScript.name);

                string gottenScriptText =
                    Script.GetScriptTextAsync(state, testScript.name).Result;
                
                Assert.AreEqual(testScript.text, gottenScriptText);

                var userScripts = Script.GetScriptNamesAsync(state).Result;
                Assert.AreEqual(1, userScripts.Count);
                Assert.AreEqual(testScript.name, userScripts[0]);

                Script.RenameScriptAsync(state, testScript.name, testScript.name + " Renamed").Wait();
                long newScriptId = Script.GetScriptIdAsync(state.MsCtxt, testScript.name + " Renamed").Result;
                Script gottenScript2 = Script.GetScriptAsync(state.MsCtxt, newScriptId).Result;
                Assert.AreEqual(testScript.name + " Renamed", gottenScript2.name);
                string scriptText2 = Script.GetScriptTextAsync(state, gottenScript2.name).Result;
                Assert.AreEqual(testScript.text, scriptText2);

                Script.DeleteScriptAsync(state, testScript.name + " Renamed").Wait();
                var userScripts2 = Script.GetScriptNamesAsync(state).Result;
                Assert.AreEqual(0, userScripts2.Count);
            }
        }

        [Test]
        public void TestDb()
        {
            HttpState State = new HttpState(null);

            Dictionary<string, IScriptContextFunction> Functions = DbScripting.DbFunctions;
            Functions["msdb.drop"].CallAsync(State, new list() { "test_table" }).Wait();

            var define_idx = new index();
            define_idx.Add("foo", "bar");
            define_idx.Add("blet", "monkey");
            Functions["msdb.define"].CallAsync(State, new list() { "test_table", 1.0, define_idx }).Wait();

            string valueResult = (string)Functions["msdb.selectValue"].CallAsync(State, new list() { "SELECT foo FROM test_table" }).Result;
            Assert.AreEqual("bar", valueResult);

            var list_params_idx = new index();
            list_params_idx.Add("@bar", "bar");
            list listResult = (list)Functions["msdb.selectList"].CallAsync(State, new list() { "SELECT blet FROM test_table WHERE foo = @bar", list_params_idx }).Result;
            Assert.AreEqual(1, listResult.Count);
            Assert.AreEqual("monkey", listResult[0]);

            index indexResult = (index)Functions["msdb.selectIndex"].CallAsync(State, new list() { "SELECT foo, blet FROM test_table" }).Result;
            Assert.AreEqual(1, indexResult.Count);
            Assert.AreEqual("monkey", indexResult["bar"]);

            list recordsResult = (list)Functions["msdb.selectRecords"].CallAsync(State, new list() { "SELECT foo, blet FROM test_table" }).Result;
            Assert.AreEqual(1, recordsResult.Count);
            index firstRecord = (index)recordsResult[0];
            Assert.AreEqual("bar", firstRecord["foo"]);
            Assert.AreEqual("monkey", firstRecord["blet"]);

            int fullRowCount = (int)(double)Functions["msdb.selectValue"].CallAsync(State, new list() { "SELECT count FROM test_table" }).Result;
            Assert.AreEqual(1, fullRowCount);

            object keysToDelete = new list() { (object)1.0 };
            Functions["msdb.delete"].CallAsync(State, new list(new[] { "test_table", keysToDelete })).Wait();

            int afterRowCount = (int)(double)Functions["msdb.selectValue"].CallAsync(State, new list() { "SELECT count FROM test_table" }).Result;
            Assert.AreEqual(0, afterRowCount);
        }
    }
}

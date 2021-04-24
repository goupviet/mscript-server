using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace metascript
{
    using list = List<object>;
    using index = ListDictionary<object, object>;

    public class ScriptLib
    {
        private const string TestUserEmail = "testscripteescripterson914@bletnevergoingtobearealemail.com";
        private const string TestUserName = "Something Completely Original With No Basis In Reality";

        private User TheTestUser;

        [SetUp]
        public void Setup()
        {
            using (var state = new HttpState(null))
            {
                if (User.GetUserIdFromEmailAsync(state.MsCtxt, TestUserEmail).Result >= 0)
                    User.DeleteUserAsync(state, TestUserEmail).Wait();

                if (User.IsNameTakenAsync(state, TestUserName).Result)
                    User.UnrecordNameTakenAsync(state, TestUserName).Wait();

                if (User.IsNameTakenAsync(state, TestUserEmail).Result)
                    User.UnrecordNameTakenAsync(state, TestUserEmail).Wait();

                Assert.IsFalse(User.IsNameTakenAsync(state, TestUserName).Result);
                Assert.IsFalse(User.IsNameTakenAsync(state, TestUserEmail).Result);

                TheTestUser = User.CreateUserAsync(state, TestUserEmail, TestUserName).Result;

                Assert.IsTrue(User.IsNameTakenAsync(state, TestUserName).Result);
                Assert.IsTrue(User.IsNameTakenAsync(state, TestUserEmail).Result);
            }
        }

        [Test]
        public void TestUser()
        {
            using (var state = new HttpState(null))
            {
                var testUserBack = User.GetUserAsync(state.MsCtxt, TheTestUser.Id).Result;
                Assert.AreEqual(TheTestUser.Id, testUserBack.Id);
                Assert.AreEqual(TheTestUser.Name, testUserBack.Name);
                Assert.AreEqual(TheTestUser.Email, testUserBack.Email);

                User.BlockUserAsync(state, TheTestUser.Email, blockSet: 1).Wait();
                Assert.IsTrue(User.GetUserAsync(state.MsCtxt, TheTestUser.Id).Result.Blocked);

                User.BlockUserAsync(state, TheTestUser.Email, blockSet: 0).Wait();
                Assert.IsFalse(User.GetUserAsync(state.MsCtxt, TheTestUser.Id).Result.Blocked);

                Assert.AreEqual(TheTestUser.Email, User.GetUserEmailAsync(state.MsCtxt, TheTestUser.Id).Result);
                Assert.AreEqual(TheTestUser.Name, User.GetUserNameAsync(state.MsCtxt, TheTestUser.Id).Result);

                Assert.AreEqual(TheTestUser.Id, User.GetUserIdFromEmailAsync(state.MsCtxt, TheTestUser.Email).Result);
                Assert.AreEqual(TheTestUser.Id, User.GetUserIdFromNameAsync(state.MsCtxt, TheTestUser.Name).Result);

                var users = User.GetUsersAsync(state.MsCtxt, TheTestUser.Name, TheTestUser.Email, "", onlyBlocked: false).Result;
                var userIds = users.Select(u => u.Id).ToHashSet();
                Assert.IsTrue(userIds.Contains(TheTestUser.Id));

                User.SetLoginTokenAsync(state, TheTestUser).Wait();
                string loginToken = User.GetUserAsync(state.MsCtxt, TheTestUser.Id).Result.LoginToken;

                var loginUserBack = User.HandleLoginTokenAsync(state, loginToken, TheTestUser.Email).Result;
                Assert.AreEqual(TheTestUser.Id, loginUserBack.Id);
                Assert.AreEqual("", User.GetUserAsync(state.MsCtxt, TheTestUser.Id).Result.LoginToken);
            }
        }

        [Test]
        public void TestEmailTokens()
        {
            using (var state = new HttpState(null))
            {
                string token1 = EmailTokens.CreateEmailTokenAsync(state.MsCtxt, TestUserEmail).Result;
                string token2 = EmailTokens.LookupEmailTokenAsync(state.MsCtxt, TestUserEmail).Result;
                Assert.AreEqual(token1, token2);

                string token3 = EmailTokens.CreateEmailTokenAsync(state.MsCtxt, TestUserEmail).Result;
                string signature = EmailTokens.ComputeSignatureAsync(state, TestUserEmail, token3).Result;
                EmailTokens.ValidateRequestAsync(state, TestUserEmail, signature).Wait();
            }
        }

        [Test]
        public void TestLogs()
        {
            using (var state = new HttpState(null))
            {
                var logCtxt = new LogContext() { ip = "0.0.0.0", userId = TheTestUser.Id };

                string traceMsg = "Test Trace Message " + Guid.NewGuid();
                Logs.LogTraceAsync(state.MsCtxt, traceMsg, logCtxt).Wait();

                string infoMsg = "Test Info Message " + Guid.NewGuid();
                Logs.LogAsync(state.MsCtxt, LogLevel.INFO, infoMsg, logCtxt).Wait();

                string errorMsg = "Test Error Message " + Guid.NewGuid();
                Logs.LogErrorAsync(state.MsCtxt, errorMsg, logCtxt).Wait();

                Assert.IsFalse(Logs.ShouldSkip(LogLevel.ERROR));

                bool traceFound = false, infoFound = false, errorFound = false;
                var logQuery =
                    new LogQuery()
                    {
                        ip = "0.0.0.0",
                        maxAgeDays = 1000,
                        maxResults = 100000,
                        userId = TheTestUser.Id,
                        like = "%"
                    };
                var results = LogEntries.GetLogEntriesAsync(state.MsCtxt, logQuery).Result;
                foreach (var result in results)
                {
                    Assert.AreEqual("0.0.0.0", result.ip);
                    if (result.msg == traceMsg) traceFound = true;
                    if (result.msg == infoMsg) infoFound = true;
                    if (result.msg == errorMsg) errorFound = true;
                }
                Assert.IsTrue(traceFound);
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
                        userId = TheTestUser.Id,
                        name = "Test Script " + Guid.NewGuid(),
                        text = ">> Hello world!"
                    };
                Script.SaveScriptAsync(state, testScript).Wait();

                Script gottenScript = Script.GetScriptAsync(state.MsCtxt, testScript.id).Result;
                Assert.AreEqual(testScript.userId, gottenScript.userId);
                Assert.AreEqual(testScript.name, gottenScript.name);

                string gottenScriptText =
                    Script.GetScriptTextAsync(state, testScript.userId, testScript.name).Result;
                
                Assert.AreEqual(testScript.text, gottenScriptText);

                var userScripts = Script.GetUserScriptNamesAsync(state, TheTestUser.Id).Result;
                Assert.AreEqual(1, userScripts.Count);
                Assert.AreEqual(testScript.name, userScripts[0]);

                Script.RenameScriptAsync(state, TheTestUser.Id, testScript.name, testScript.name + " Renamed").Wait();
                long newScriptId = Script.GetScriptIdAsync(state.MsCtxt, TheTestUser.Name, testScript.name + " Renamed").Result;
                Script gottenScript2 = Script.GetScriptAsync(state.MsCtxt, newScriptId).Result;
                Assert.AreEqual(testScript.userId, gottenScript2.userId);
                Assert.AreEqual(testScript.name + " Renamed", gottenScript2.name);
                string scriptText2 = Script.GetScriptTextAsync(state, TheTestUser.Id, gottenScript2.name).Result;
                Assert.AreEqual(testScript.text, scriptText2);

                Script.DeleteScriptAsync(state, TheTestUser.Id, testScript.name + " Renamed").Wait();
                var userScripts2 = Script.GetUserScriptNamesAsync(state, TheTestUser.Id).Result;
                Assert.AreEqual(0, userScripts2.Count);
            }
        }

        [Test]
        public void TestSessions()
        {
            using (var state = new HttpState(null))
            {
                Session.ResetAsync(state.MsCtxt).Wait();
                Assert.AreEqual(-1, Session.GetSessionAsync(state.MsCtxt, "foobar").Result);
                string cookie = Session.CreateSessionAsync(state, TheTestUser.Id).Result;
                Assert.AreEqual(TheTestUser.Id, Session.GetSessionAsync(state.MsCtxt, cookie).Result);
                Session.ForceUserOutAsync(state, TheTestUser.Id).Wait();
                Assert.AreEqual(-1, Session.GetSessionAsync(state.MsCtxt, cookie).Result);
                Session.DeleteSessionAsync(state, cookie).Wait();
            }
        }

        [Test]
        public void TestDb()
        {
            HttpState State = new HttpState(null);
            State.UserId = TheTestUser.Id;

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

using System;

using NUnit.Framework;

namespace metascript
{
    public class ErrorLogTests
    {
        [SetUp]
        public void Setup()
        {
            HttpState.DbConnStr = "Data Source=[UserRoaming]/mscript-tests.db";
        }

        [Test]
        public void TestErrorLog()
        {
            using (var state = new HttpState(null))
            using (var ctxt = state.MsCtxt)
            {
                for (int t = 1; t <= 3; ++t)
                {
                    ErrorLog.ClearAsync(ctxt).Wait();

                    ErrorLog.LogAsync(ctxt, "foo foo bar").Wait();
                    ErrorLog.LogAsync(ctxt, "blet monkey").Wait();

                    {
                        var logEntries = ErrorLog.QueryAsync(ctxt, 10).Result;
                        Assert.AreEqual(2, logEntries.Count);
                        bool found = false;
                        DateTime foundDt = DateTime.MinValue;
                        foreach (var entry in logEntries)
                        {
                            if (entry.msg == "foo foo bar")
                            {
                                found = true;
                                foundDt = entry.when;
                                break;
                            }
                        }
                        Assert.IsTrue(found);
                        Assert.IsTrue((DateTime.UtcNow - foundDt).TotalSeconds < 10);
                    }
                }
            }
        }
    }
}

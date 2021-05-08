using System;

using NUnit.Framework;

namespace metascript
{
    public class ErrorLogTests
    {
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
                        var logEntries = ErrorLog.QueryAsync(ctxt, "%foo%", 10).Result;
                        Assert.AreEqual(1, logEntries.Count);
                        Assert.AreEqual("foo foo bar", logEntries[0].msg);
                        Assert.IsTrue((DateTime.UtcNow - logEntries[0].when).TotalSeconds < 10);
                    }

                    {
                        var logEntries = ErrorLog.QueryAsync(ctxt, "blet%", 10).Result;
                        Assert.AreEqual(1, logEntries.Count);
                        Assert.AreEqual("blet monkey", logEntries[0].msg);
                        Assert.IsTrue((DateTime.UtcNow - logEntries[0].when).TotalSeconds < 10);
                    }
                }
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net;

using metastrings;

namespace metascript
{
    public class PageFinishException : Exception
    {
    }

    public class HttpState : IDisposable
    {
        public HttpState(HttpListenerContext httpCtxt)
        {
            HttpCtxt = httpCtxt;

            if (string.IsNullOrWhiteSpace(DbConnStr))
                throw new MException("Database connection string not provided");
        }

        public void Dispose()
        {
            if (m_msCtxt != null)
            {
                m_msCtxt.Dispose();
                m_msCtxt = null;
            }

            if (HttpCtxt != null)
            {
                HttpCtxt.Response.OutputStream.Dispose();
                HttpCtxt = null;
            }
        }

        public static string DbConnStr;

        public Context MsCtxt
        {
            get
            {
                if (m_msCtxt == null)
                {
                    if (string.IsNullOrWhiteSpace(DbConnStr))
                        throw new MException("HttpState.DbConnStr not set");
                    m_msCtxt = new Context(DbConnStr);
                }
                return m_msCtxt;
            }
        }
        private Context m_msCtxt;

        public HttpListenerContext HttpCtxt;

        public bool ReadInputYet = false;
        public bool WrittenOutput = false;

        public async Task WriteResponseAsync(string str)
        {
            using (var writer = new StreamWriter(HttpCtxt.Response.OutputStream, leaveOpen: true))
                await writer.WriteAsync(str).ConfigureAwait(false);
        }

        public Dictionary<string, string> RequestCookies
        {
            get
            {
                if (m_requestCookies == null)
                {
                    m_requestCookies = new Dictionary<string, string>();
                    if (HttpCtxt != null)
                    {
                        ParseAjaxCookieJar
                        (
                            HttpCtxt.Request.Headers["Ajax-Cookies"], 
                            RequestCookies
                        );
                    }
                }
                return m_requestCookies;
            }
        }
        private Dictionary<string, string> m_requestCookies;

        public async Task<string> GetRequestPostAsync()
        {
            if (m_requestPost != null)
                return m_requestPost;

            if (HttpCtxt == null || HttpCtxt.Request.HttpMethod != "POST")
                return "";

            if (ReadInputYet)
                throw new MException("Input already read");
            ReadInputYet = true;

            using (var reader = new StreamReader(HttpCtxt.Request.InputStream, leaveOpen: true))
                m_requestPost = await reader.ReadToEndAsync().ConfigureAwait(false);
            return m_requestPost;
        }
        private string m_requestPost;

        public async Task FinishWithMessageAsync(string message)
        {
            if (WrittenOutput)
                return; // too late
            WrittenOutput = true;

            HttpCtxt.Response.ContentType = "application/json";

            var outputs = new Dictionary<string, string>();

            outputs["ajaxFinish"] = "true";
            outputs["message"] = message;

            string payload = MUtils.ObjectToString(outputs);

            using (var writer = new StreamWriter(HttpCtxt.Response.OutputStream, leaveOpen: true))
                await writer.WriteAsync(payload).ConfigureAwait(false);

            throw new PageFinishException();
        }

        private static void ParseAjaxCookieJar(string cookieJar, Dictionary<string, string> output)
        {
            if (string.IsNullOrWhiteSpace(cookieJar))
                return;

            foreach (var cookie in cookieJar.Split(';'))
            {
                int idx = cookie.IndexOf('=');
                if (idx <= 0)
                    continue;
                output[cookie.Substring(0, idx)] =
                    WebUtility.UrlDecode(cookie.Substring(idx + 1));
            }
        }
    }
}

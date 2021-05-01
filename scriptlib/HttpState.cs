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

        public Context MsCtxt
        {
            get
            {
                if (m_msCtxt == null)
                    m_msCtxt = new Context();
                return m_msCtxt;
            }
        }
        private Context m_msCtxt;

        public HttpListenerContext HttpCtxt;

        public long UserId = -1;
        public string ReturnPage;

        public bool ReadInput = false;
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

            if (ReadInput)
                throw new MException("Input already read");
            ReadInput = true;

            using (var reader = new StreamReader(HttpCtxt.Request.InputStream, leaveOpen: true))
                m_requestPost = await reader.ReadToEndAsync().ConfigureAwait(false);
            return m_requestPost;
        }
        public string m_requestPost;

        public async Task<Dictionary<string, string>> GetRequestFieldsAsync()
        {
            if (m_requestFields == null)
            {
                if (ReadInput)
                    throw new MException("Input already read");
                ReadInput = true;

                string json;
                if (HttpCtxt == null)
                {
                    json = "{}";
                }
                else
                {
                    using (var reader = new StreamReader(HttpCtxt.Request.InputStream, leaveOpen: true))
                        json = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                m_requestFields = MUtils.StringToObject<Dictionary<string, string>>(json);
            }
            return m_requestFields;
        }
        private Dictionary<string, string> m_requestFields;

        public async Task SetFinalStatusAsync(int statusCode, string statusDescription)
        {
            if (WrittenOutput)
                return; // too late
            WrittenOutput = true;

            HttpCtxt.Response.ContentType = "text/plain";
            HttpCtxt.Response.StatusCode = statusCode;

            using (var writer = new StreamWriter(HttpCtxt.Response.OutputStream, leaveOpen: true))
                await writer.WriteAsync(statusDescription).ConfigureAwait(false);
        }

        public void SetResponseSession(string key)
        {
            SetResponseCookie("session", key);
        }

        public async Task FinishWithMessageAsync(string page, string msg)
        {
            SetResponseCookie("message", msg);
            await FinishAsync(page).ConfigureAwait(false);
        }

        private void SetResponseCookie(string name, string value)
        {
            if (m_responseCookies == null)
                m_responseCookies = new Dictionary<string, string>();
            m_responseCookies[name] = value;
        }
        private Dictionary<string, string> m_responseCookies;

        public async Task FinishAsync(string page)
        {
            if (WrittenOutput)
                return; // too late
            WrittenOutput = true;

            string payload = GetFinishPayload(page);

            using (var writer = new StreamWriter(HttpCtxt.Response.OutputStream, leaveOpen: true))
                await writer.WriteAsync(payload).ConfigureAwait(false);

            throw new PageFinishException();
        }

        public void Finish(string page)
        {
            if (WrittenOutput)
                return; // too late
            WrittenOutput = true;

            string payload = GetFinishPayload(page);

            using (var writer = new StreamWriter(HttpCtxt.Response.OutputStream, leaveOpen: true))
               writer.Write(payload);

            throw new PageFinishException();
        }

        private string GetFinishPayload(string page)
        {
            HttpCtxt.Response.ContentType = "application/json";

            var outputs = new Dictionary<string, string>();

            outputs["ajaxFinish"] = "true";
            outputs["redirect"] = page;

            if (m_responseCookies != null)
            {
                if (m_responseCookies.ContainsKey("session"))
                    outputs["session"] = m_responseCookies["session"];

                if (m_responseCookies.ContainsKey("message"))
                    outputs["message"] = m_responseCookies["message"];
            }

            var payload = MUtils.ObjectToString(outputs);
            return payload;
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

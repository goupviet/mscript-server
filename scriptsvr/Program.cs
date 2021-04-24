using System;
using System.Threading.Tasks;
using System.Text;
using System.Net;

namespace metascript
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 9090;
            /* Unused
            for (int a = 0; a < args.Length; ++a)
            {
                string cur = args[a].TrimStart('-');
                string next = (a + 1) < args.Length ? args[a + 1] : null;
                switch (cur)
                {
                    case "port":
                        port = int.Parse(next);
                        break;
                }
            }
            */
            Console.WriteLine("Port: {0}", port);

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{port}/");
            listener.Start();
            while (true)
            {
                var ctxt = listener.GetContext();
                Task.Run(async () => await HandleClientAsync(ctxt).ConfigureAwait(false));
            }
        }

        public static async Task HandleClientAsync(HttpListenerContext httpCtxt)
        {
#if !DEBUG
            try
#endif
            {
                using (var state = new HttpState(httpCtxt))
                {
                    httpCtxt.Response.ContentEncoding = Encoding.UTF8;
                    httpCtxt.Response.Headers.Add("Cache-Control", "no-store");
                    httpCtxt.Response.Headers.Add("Pragma", "no-cache");
                    httpCtxt.Response.Headers.Add("Content-Type", "text/html");

                    httpCtxt.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                    httpCtxt.Response.AppendHeader("Access-Control-Allow-Methods", "GET, POST");
                    httpCtxt.Response.AppendHeader("Access-Control-Allow-Headers", "Authorization, Ajax-Cookies");

                    if (httpCtxt.Request.HttpMethod == "OPTIONS") // CORS OPTIONS request completes
                        return;

                    string path;
                    {
                        path = httpCtxt.Request.Url.PathAndQuery.TrimStart('/');
                        int question = path.IndexOf('?');
                        if (question > 0)
                            path = path.Substring(0, question);
                        path = path.ToLower();
                    }
                    Console.WriteLine("Request: {0} - {1} {2}", 
                                      WebUtils.GetClientIpAddress(state), 
                                      state.HttpCtxt.Request.HttpMethod, 
                                      path);

                    IPage page;
                    switch (path)
                    {
                        case "savescript":
                            page = new SaveScript();
                            break;

                        case "execute":
                            page = new ExecuteScript();
                            break;

                        case "getscripttext":
                            page = new GetScriptText();
                            break;

                        case "getscriptnames":
                            page = new GetScriptNames();
                            break;

                        case "getloggedinusername":
                            page = new GetLoggedInUsername();
                            break;

                        case "registeremail":
                            page = new RegisterEmail();
                            break;

                        case "logout":
                            page = new Logout();
                            break;

                        case "login":
                            page = new Login();
                            break;

                        case "login2":
                            page = new Login2();
                            break;

                        case "signup":
                            page = new SignUp();
                            break;

                        case "signup2":
                            page = new SignUp2();
                            break;

                        case "deletescript":
                            page = new DeleteScript();
                            break;

                        case "renamescript":
                            page = new RenameScript();
                            break;

                        default:
                            await WebUtils.LogInfoAsync(state, $"Page not found: {path}");
                            httpCtxt.Response.StatusCode = 404;
                            return;
                    }

                    Exception capturedException;
                    try
                    {
                        await page.HandleRequestAsync(state);
                        Console.WriteLine("Handler completes");
                        return;
                    }
                    catch (PageFinishException)
                    {
                        Console.WriteLine("Handler finishes");
                        return;
                    }
                    catch (Exception pageExp)
                    {
                        Console.WriteLine("Handler ERROR");
                        capturedException = pageExp;
                    }
                    await Errors.HandleErrorAsync(state, capturedException);
                }
            }
#if !DEBUG
            catch (Exception exp)
            {
                Console.WriteLine("Unhandled EXCEPTION: " + exp);
            }
#endif
        }
    }
}

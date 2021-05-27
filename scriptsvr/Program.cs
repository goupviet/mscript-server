using System;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace metascript
{
    /// <summary>
    /// Web server class based on HttpListener.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Put the DB somewhere friendly.  Roaming is a bad choice for a store app.
            HttpState.DbConnStr = "Data Source=[MyDocuments]/mscript/mscript.db";

            int port = 16914;
            bool localOnly = true;
            for (int a = 0; a < args.Length; ++a)
            {
                string cur = args[a].TrimStart('-');
                string next = (a + 1) < args.Length ? args[a + 1] : null;
                switch (cur)
                {
                    case "port":
                        port = int.Parse(next);
                        ++a;
                        break;

                    case "allowRemoteAccess":
                        localOnly = false;
                        break;
                }
            }
            Console.WriteLine("Port: {0}", port);

            // Kill off any processes already running.
            // There can be only one!
            try
            {
                Console.Write("Getting processes...");
                var processes = Process.GetProcesses();
                Console.WriteLine($" {processes.Length} found");
                foreach (var process in processes)
                {
                    if (Path.GetFileNameWithoutExtension(process.ProcessName) == "scriptsvr")
                    {
                        if (process.Id != Process.GetCurrentProcess().Id)
                        {
                            Console.WriteLine($"Killing process {process.Id}");
                            process.Kill(true);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("Killing existing processes failed, bailing");
                Console.WriteLine($"{exp.GetType().FullName}: {exp.Message}");
                return;
            }

            // Start listening for requests.
            Console.WriteLine("Listening...");
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            try
            {
                listener.Start();
            }
            catch (Exception exp)
            {
                Console.WriteLine("Starting listening failed, bailing");
                Console.WriteLine($"{exp.GetType().FullName}: {exp.Message}");
                return;
            }

            // Process requets.
            Console.WriteLine("Processing...");
            while (true)
            {
                var ctxt = listener.GetContext();
                if (localOnly && !ctxt.Request.IsLocal) // a little security
                {
                    Console.WriteLine("Ignoring non-local request");
                    continue;
                }

                // Fire off a task to handle the request.
                Task.Run(async () => await HandleClientAsync(ctxt).ConfigureAwait(false));
            }
        }

        private static async Task HandleClientAsync(HttpListenerContext httpCtxt)
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
                    Console.WriteLine("Request: {0} {1}", 
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

                        case "deletescript":
                            page = new DeleteScript();
                            break;

                        case "renamescript":
                            page = new RenameScript();
                            break;

                        case "geterrorlog":
                            page = new GetErrorLog();
                            break;

                        default:
                            await ErrorLog.LogAsync(state.MsCtxt, $"Page not found: {path}").ConfigureAwait(false);
                            httpCtxt.Response.StatusCode = 404;
                            return;
                    }

                    Exception capturedException;
                    try
                    {
                        await page.HandleRequestAsync(state).ConfigureAwait(false);
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
                    await Errors.HandleErrorAsync(state, capturedException).ConfigureAwait(false);
                }
            }
#if !DEBUG
            catch (Exception exp)
            {
                if (!(exp is PageFinishException))
                    Console.WriteLine("Unhandled EXCEPTION: " + exp);
            }
#endif
        }
    }
}

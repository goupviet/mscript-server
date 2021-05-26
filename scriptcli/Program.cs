using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace metascript
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                if (args.Length != 2)
                {
                    Console.WriteLine("Usage: scriptcli <input file path> <-w or output file path>");
                    Console.WriteLine("Use -w to open the result in a web browser instead of an output file");
                    return 0;
                }

                HttpState.DbConnStr = "Data Source=[MyDocuments]/mscript/mscript.db";

                string scriptText = File.ReadAllText(args[0], Encoding.UTF8);
                var symbols = new SymbolTable();
                using (HttpState state = new HttpState(null))
                using (var outputStream = new MemoryStream())
                using
                (
                    var processor =
                        new ScriptProcessor
                        (
                            scriptText,
                            symbols,
                            outputStream,
                            state,
                            DbScripting.DbFunctions
                        )
                    )
                {
                    ScriptException collectedExp = null;
                    try
                    {
                        await processor.ProcessAsync().ConfigureAwait(false);
                    }
                    catch (ScriptException exp)
                    {
                        collectedExp = exp;
                    }

                    if (collectedExp != null)
                    {
                        using (var sw = new StreamWriter(outputStream, Encoding.UTF8))
                        {
                            sw.Write
                            (
                                $"ERROR: {collectedExp.Message}\n" +
                                $"Line {collectedExp.LineNumber}\n" +
                                $"{collectedExp.Line}"
                            );
                        }
                        return 1;
                    }

                    if (args[1] == "-w")
                    {
                        string tempFilePath = Path.Combine(Path.GetTempPath(), "mscript-temp-output.html");
                        File.WriteAllBytes(tempFilePath, outputStream.ToArray());
                        ProcessStartInfo startInfo = new ProcessStartInfo(tempFilePath);
                        startInfo.UseShellExecute = true;
                        Process.Start(startInfo);
                    }
                    else
                    {
                        File.WriteAllBytes(args[1], outputStream.ToArray());
                    }
                    return 0;
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("EXCEPTION: {0}", exp);
                return 1;
            }
        }
    }
}

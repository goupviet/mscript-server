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
                if (args.Length == 0)
                {
                    Console.WriteLine("Usage: scriptcli <input file path> [-w or output file path]");
                    Console.WriteLine("Use -w to open the output in a web browser instead of writing it to a file");
                    Console.WriteLine("If no second parameter, the output is written to the console");
                    return 0;
                }

                HttpState.DbConnStr = "Data Source=[MyDocuments]/mscript/mscript.db";

                string inputFilePath = args[0];
                if (!File.Exists(inputFilePath))
                {
                    Console.WriteLine("ERROR: File does not exist: {0}", inputFilePath);
                    return 1;
                }

                string scriptText = File.ReadAllText(inputFilePath, Encoding.UTF8);

                var symbols = new SymbolTable();

                using (var outputStream = new MemoryStream())
                using (HttpState state = new HttpState(null))
                using
                (
                    var processor =
                        new ScriptProcessor
                        (
                            scriptText,
                            symbols,
                            outputStream,
                            "cliScript",
                            "cliAction",
                            state,
                            DbScripting.DbFunctions
                        )
                    )
                {
                    ScriptException collectedExp = null;
                    try
                    {
                        await processor.ProcessAsync();
                    }
                    catch (ScriptException exp)
                    {
                        collectedExp = exp;
                    }

                    int retVal = 0;

                    if (collectedExp != null)
                    {
                        Console.WriteLine($"ERROR: {collectedExp.Message}");
                        Console.WriteLine($"Line {collectedExp.LineNumber}");
                        Console.WriteLine($"{collectedExp.Line}");
                        Console.WriteLine();
                        retVal = 1;
                    }

                    string outputOption = args.Length >= 2 ? args[1] : null;
                    if (outputOption == null)
                    {
                        string outputStr = Encoding.UTF8.GetString(outputStream.ToArray());
                        Console.WriteLine(outputStr);
                    }
                    else if (outputOption == "-w")
                    {
                        string tempFilePath = Path.Combine(Path.GetTempPath(), "scriptcli-temp-output.html");
                        File.WriteAllBytes(tempFilePath, outputStream.ToArray());
                        ProcessStartInfo startInfo = new ProcessStartInfo(tempFilePath);
                        startInfo.UseShellExecute = true;
                        Process.Start(startInfo);
                    }
                    else
                        File.WriteAllBytes(outputOption, outputStream.ToArray());

                    return retVal;
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

using System;

namespace metascript
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("RTFM!");
                return;
            }
			
            string cmd = args[0].Trim();
            string arg = "";
            if (args.Length > 1)
                arg = args[1];

            try
            {
                using (var state = new HttpState(null))
                {
                    switch (cmd)
                    {
						default:
							Console.WriteLine("Nothing to see here.  Move along, move along.");
							break;
                    }
                }
            }
            catch (Exception exp)
            {
                while (exp.InnerException != null)
                    exp = exp.InnerException;
                Console.WriteLine("EXCEPTION: " + exp);
            }
        }
    }
}

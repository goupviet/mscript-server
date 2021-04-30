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
                        case "deleteuser":
                            {
                                string email = arg;
                                Console.WriteLine("Email: " + email);
                                if (!Email.IsEmailValid(email))
                                {
                                    Console.WriteLine("ERROR: Invalid email address!");
                                    return;
                                }
                                Console.Write("Press [Enter] to delete - " + email + " - : ");
                                Console.ReadLine();
                                if (User.GetUserIdFromEmailAsync(state.MsCtxt, email).Result < 0)
                                {
                                    Console.WriteLine("ERROR: User not found with email " + email);
                                    return;
                                }
                                User.DeleteUserAsync(state, email).Wait();
                                Console.WriteLine("User deleted.");
                            }
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

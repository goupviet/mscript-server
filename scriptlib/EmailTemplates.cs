using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace metascript
{
    public static class EmailTemplates
    {
        public static Tuple<string, string> Templates
        {
            get
            {
                EnsureInit();
                return sm_templateTuple;
            }
        }

        private static void EnsureInit()
        {
            if (sm_bInit)
                return;
            lock (sm_initLock)
            {
                if (sm_bInit)
                    return;

                string exeDirPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                {
                    string templateFilePath = Path.Combine(exeDirPath, "email.txt");
                    sm_textTemplate = File.ReadAllText(templateFilePath, Encoding.UTF8);
                }

                {
                    string templateFilePath = Path.Combine(exeDirPath, "email.html");
                    sm_htmlTemplate = File.ReadAllText(templateFilePath, Encoding.UTF8);
                }

                sm_templateTuple = new Tuple<string, string>(sm_textTemplate, sm_htmlTemplate);

                sm_bInit = true;
            }
        }
        private static bool sm_bInit = false;
        private static object sm_initLock = new object();

        private static string sm_textTemplate;
        private static string sm_htmlTemplate;

        private static Tuple<string, string> sm_templateTuple;
    }
}

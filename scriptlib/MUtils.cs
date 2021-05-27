using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace metascript
{
    /// <summary>
    /// Utility functions for getting things done.
    /// </summary>
    public static class MUtils
    {
        /// <summary>
        /// SHA-256 wrapper.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HashStr(string str)
        {
            StringBuilder sb = new StringBuilder(64);
            using (var sha = SHA256.Create())
            {
                byte[] crypto = sha.ComputeHash(Encoding.UTF8.GetBytes(str));
                foreach (byte theByte in crypto)
                    sb.Append(theByte.ToString("X2"));
            }
            return sb.ToString();
        }
    }
}

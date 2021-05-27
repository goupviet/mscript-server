using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;

using Newtonsoft.Json;

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

        /// <summary>
        /// Object => JSON
        /// </summary>
        public static string ObjectToString(object obj)
        {
            StringBuilder sb = new StringBuilder();
            using (var textWriter = new StringWriter(sb))
                sm_serializer.Serialize(textWriter, obj);
            return sb.ToString();
        }

        /// <summary>
        /// JSON => Object
        /// </summary>
        public static T StringToObject<T>(string str)
        {
            using (var textReader = new StringReader(str))
            using (var jsonReader = new JsonTextReader(textReader))
                return sm_serializer.Deserialize<T>(jsonReader);
        }

        private static JsonSerializer sm_serializer = new JsonSerializer();
    }
}

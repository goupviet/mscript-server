using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;

using Newtonsoft.Json;

namespace metascript
{
    public static class MUtils
    {
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

        public static string ObjectToString(object obj)
        {
            StringBuilder sb = new StringBuilder();
            using (var textWriter = new StringWriter(sb))
                sm_serializer.Serialize(textWriter, obj);
            return sb.ToString();
        }

        public static T StringToObject<T>(string str)
        {
            using (var textReader = new StringReader(str))
            using (var jsonReader = new JsonTextReader(textReader))
                return sm_serializer.Deserialize<T>(jsonReader);
        }

        private static JsonSerializer sm_serializer = new JsonSerializer();
    }
}

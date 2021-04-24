using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Configuration;

using Newtonsoft.Json;

namespace metascript
{
    public static class MUtils
    {
        public static long GetInt64(object obj)
        {
            if (obj == null || obj == DBNull.Value)
                return -1;
            else
                return Convert.ToInt64(obj);
        }

        public static int GetInt32(object obj)
        {
            if (obj == null || obj == DBNull.Value)
                return -1;
            else
                return Convert.ToInt32(obj);
        }

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

        public static string GetAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? "";
        }

        public static int GetAppSettingInt(string key)
        {
            string setting = GetAppSetting(key);

            int val;
            if (!int.TryParse(setting, out val))
                throw new MException($"Invalid/Missing int config setting: {key} - {setting}");

            return val;
        }

        public static bool GetAppSettingBool(string key)
        {
            string setting = GetAppSetting(key);

            bool val;
            if (!bool.TryParse(setting, out val))
                throw new MException($"Invalid/Missing bool config setting: {key} - {setting}");

            return val;
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

        public static string CreateToken(int desiredLength)
        {
            string token = HashStr($"{Guid.NewGuid()} {DateTime.UtcNow.Ticks} sometimes I like corned beef hash");
            token = token.Substring(0, Math.Min(token.Length, desiredLength));
            return token;
        }

        private static JsonSerializer sm_serializer = new JsonSerializer();
    }
}

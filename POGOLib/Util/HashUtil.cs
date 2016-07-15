using System;
using System.Security.Cryptography;
using System.Text;

namespace POGOLib.Util
{
    internal static class HashUtil
    {

        public static string HashMD5(string text)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var textToHash = Encoding.Default.GetBytes(text);
                var result = md5.ComputeHash(textToHash);

                return BitConverter.ToString(result).Replace("-", "");
            }
        }

    }
}

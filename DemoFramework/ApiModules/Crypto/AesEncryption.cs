using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ApiModules.Crypto
{
    public static class AesEncryption
    {
        public static string Encrypt(string plainText, string key)
        {
            var keyBytes = GetKeyBytes(key);

            using (var aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText, string key)
        {
            var keyBytes = GetKeyBytes(key);
            var fullData = Convert.FromBase64String(cipherText);

            using (var aes = Aes.Create())
            {
                aes.Key = keyBytes;

                var iv = new byte[16];
                Array.Copy(fullData, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(fullData, iv.Length, fullData.Length - iv.Length))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private static byte[] GetKeyBytes(string key)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
        }
    }
}
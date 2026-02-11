using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace FreshFarmSecureApp.Services
{
    public class EncryptionService
    {
        private readonly byte[] _key;

        public EncryptionService(IConfiguration configuration)
        {
            _key = Convert.FromBase64String(configuration["EncryptionKey"]);
        }

        public string Encrypt(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();
            ICryptoTransform encryptor = aes.CreateEncryptor();
            using MemoryStream ms = new();
            ms.Write(aes.IV, 0, aes.IV.Length);  // Prefix IV
            using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
            using (StreamWriter sw = new(cs)) sw.Write(plainText);
            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);
            using Aes aes = Aes.Create();
            aes.Key = _key;
            byte[] iv = new byte[16];
            Array.Copy(buffer, 0, iv, 0, 16);
            aes.IV = iv;
            ICryptoTransform decryptor = aes.CreateDecryptor();
            using MemoryStream ms = new(buffer, 16, buffer.Length - 16);
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }
    }
}
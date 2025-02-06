using System.Security.Cryptography;
using System.Text;

namespace ImitationOfLife
{
    public static class Config
    {
        public static string TempDirectoryPath => StartFromLocation("tempdir");
        public static string TokenPath => StartFromLocation("token.dat");
        public static string TokenKeyPath => StartFromLocation("tokenKey.dat");

        public static byte[] IV { get; } = Encoding.UTF8.GetBytes("lobotomy12345678");
        
        public static void Initialize()
        {
            CreateKeyFile();
            CreateDirectory();
            CreateTokenFile();
        }

        public static void SetToken(string token)
        {
            CreateTokenFile();
            File.WriteAllText(TokenPath, Encrypt(token));
        }
        public static void SetKey(string key)
        {
            CreateKeyFile();
            File.WriteAllText(TokenKeyPath, key);
        }

        public static string LoadKey() => File.ReadAllText(TokenKeyPath);
        public static string LoadToken() => Decrypt(File.ReadAllText(TokenPath));
        
        private static void CreateDirectory()
        {
            if (!Directory.Exists(TempDirectoryPath))
            {
                Directory.CreateDirectory(TempDirectoryPath);
            }
        }
        private static void CreateTokenFile()
        {
            if (!File.Exists(TokenPath))
            {
                File.Create(TokenPath).Dispose();
            }
        }
        private static void CreateKeyFile()
        {
            if (!File.Exists(TokenKeyPath))
            {
                File.Create(TokenKeyPath).Dispose();
            }
        }

        private static string StartFromLocation(string path) => Path.Combine(Environment.CurrentDirectory, path);

        private static string Encrypt(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = GetKeyBytes();
            aes.IV = IV;

            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using StreamWriter writer = new(cs);
            
            writer.Write(plainText);
            writer.Flush();
            cs.FlushFinalBlock();

            return Convert.ToBase64String(ms.ToArray());
        }
        private static string Decrypt(string cipherText)
        {
            using Aes aes = Aes.Create();
            aes.Key = GetKeyBytes();
            aes.IV = IV;

            using MemoryStream ms = new(Convert.FromBase64String(cipherText));
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader reader = new(cs);
            return reader.ReadToEnd();
        }
        private static byte[] GetKeyBytes() => Encoding.UTF8.GetBytes(LoadKey());

    }
}
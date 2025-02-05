namespace ImitationOfLife
{
    public static class Config
    {
        public static string TempDirectoryPath => Path.Combine(Environment.CurrentDirectory, "tempdir");
        public static string ConfigFilePath => Path.Combine(Environment.CurrentDirectory, "token.txt");

        public static void SetBotToken(string newToken) => File.WriteAllText(ConfigFilePath, newToken);
        public static string GetBotToken()
        {
            if (!File.Exists(ConfigFilePath))
            {
                File.Create(ConfigFilePath).Dispose();
                Console.WriteLine($"Created config file: {ConfigFilePath}");
            }

            return File.ReadAllText(ConfigFilePath);
        }

        public static string GetTempDirectory()
        {
            if (!Directory.Exists(TempDirectoryPath))
            {
                Directory.CreateDirectory(TempDirectoryPath);
                Console.WriteLine($"Created directory: {TempDirectoryPath}");
            }

            return TempDirectoryPath;
        }
    }
}
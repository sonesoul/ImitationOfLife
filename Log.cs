using Telegram.Bot.Types;

namespace ImitationOfLife
{
    public static class Log
    {
        public class LogInfo(string message)
        {
            public string Message { get; } = message;
            public DateTime Time { get; } = DateTime.Now;
            public string FullMessage => $"[{Time.TimeOfDay}] {Message}";
        }

        public static event Action<LogInfo> Created;
        
        public static void Create(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            var temp = Console.ForegroundColor;
            Console.ForegroundColor = color;

            LogInfo record = new(text);

            Console.WriteLine(text);
            Created?.Invoke(record);

            Console.ForegroundColor = temp;
        }
        public static void FromMessage(Message msg, string message) => Create($"{msg.From}: {message}");
    }
}
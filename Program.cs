namespace ImitationOfLife
{
    public static class Program
    {
        private static class ProgramCommands
        {
            private readonly static Command[] commands =
            [
                new("run", Commands.Run, "initialize and unlock bot"),
                new("init", Bot.Initialize, "initialize bot (no unlocking)"),
                new("break", Bot.Break, "break bot instance"),
                new("lock", Bot.Lock, "lock bot commands"),
                new("unlock", Bot.Unlock, "unlock bot commands"),
                new("help", Commands.Help, "show this info"),
                new("settoken", Commands.SetToken, "set bot token"),
                new("gettoken", () => Console.WriteLine(Config.GetBotToken()), "get current bot token"),
                new("clear", Console.Clear, "clear the console"),
                new("exit", Commands.Exit, "close the program"),
            ];

            private static class Commands
            {
                public static void Run()
                {
                    Bot.Initialize();
                    Bot.Unlock();
                    Console.WriteLine();
                }
                public static void Exit() => Environment.Exit(0);
                public static void Help()
                {
                    Console.WriteLine("------------");
                    var names = commands.Select(c => (c.Key, c.Caption)).ToList();

                    foreach ((string key, string caption) in names)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("  " + key);
                        Console.ResetColor();
                        Console.Write(" - " + caption + '\n');
                    }

                    Console.WriteLine("------------\n");
                }
                public static void SetToken()
                {
                    Console.Write("Enter bot token: ");
                    Config.SetBotToken(Console.ReadLine().Trim());
                }
            }
            public static void HandleInput(string input)
            {
                foreach (var item in commands)
                {
                    if (input.Trim().Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Action();
                        break;
                    }
                }
            }
            readonly struct Command(string key, Action action, string caption)
            {
                public Action Action { get; init; } = action;
                public string Key { get; init; } = key;
                public string Caption { get; init; } = caption;
            }
        }

        private static void Main(string[] args)
        {
            ProgramCommands.HandleInput("help");
            
            while (true)
            {
                try
                {
                    ProgramCommands.HandleInput(Console.ReadLine());
                }
                catch (Exception ex)
                {
                    Log.Create(ex.Message, ConsoleColor.Red);
                }
            }
        }
    }
}
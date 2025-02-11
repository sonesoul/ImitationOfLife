﻿using System.Text;

namespace ImitationOfLife
{
    public static class Program
    {
        private static class ProgramCommands
        {
            private readonly static Command[] commands =
            [
                new("run", s => Commands.Run(), "initialize and unlock bot"),
                new("init", s => Bot.Initialize(), "initialize bot (no unlocking)"),
                new("break", s => Bot.Break(), "break bot instance"),
                new("lock", s => Bot.Lock(), "lock bot commands"),
                new("unlock", s => Bot.Unlock(), "unlock bot commands"),
                new("help", s => Commands.Help(), "show this info"),
                
                new("settoken", Config.SetToken, "set bot token"),
                new("gettoken", s => Console.WriteLine(Config.LoadToken()), "get current bot token"),

                new("setkey", Config.SetKey, "set token encryption key"),
                new("getkey", s => Console.WriteLine(Config.LoadKey()), "get token encryption key"),
                
                new("clear", s=> Console.Clear(), "clear the console"),
                new("exit", s => Commands.Exit(), "close the program"),
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
            }
            public static void HandleInput(string input)
            {
                string[] inputs = input.Split(' ');
                string command = inputs[0].Trim();
                string args = string.Join(" ", inputs[1..]);

                foreach (var item in commands)
                {
                    if (command.Equals(item.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Action(args);
                        break;
                    }
                }
            }
            readonly struct Command(string key, Action<string> action, string caption)
            {
                public Action<string> Action { get; init; } = action;
                public string Key { get; init; } = key;
                public string Caption { get; init; } = caption;
            }
        }

        private static void Main(string[] args)
        {
            Config.Initialize();

            ProgramCommands.HandleInput("help");

            foreach (var item in args)
            {
                ProgramCommands.HandleInput(item);
            }

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
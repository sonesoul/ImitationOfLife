global using static ImitationOfLife.Bot.Instance;
global using ImitationOfLife.Interfaces;

using Telegram.Bot.Types;
using Telegram.Bot;

namespace ImitationOfLife
{
    public static class Bot
    {
        public static class Instance
        {
            public static TelegramBotClient Client { get; private set; }
            public static void Create() => Client = new(Config.GetBotToken());
        }

        private static bool isLocked = false;
        private static bool isRunning = false;

        private static CancellationTokenSource cts;

        public static void Initialize()
        {
            if (isRunning)
                return;

            Create();
            StartBot();

            isRunning = true;
            Log.Create("Bot initialized");
        }
        public static void Break()
        {
            if (!isRunning)
                return;

            cts.Cancel();

            isLocked = true;
            isRunning = false;
        }
        public static void Unlock()
        {
            if (!isLocked)
                return;

            isLocked = false;
            Log.Create("Commands unlocked\n");
        }
        public static void Lock()
        {
            if (isLocked)
                return;

            isLocked = true;
            Log.Create("Commands locked");
        }

        private static void StartBot()
        {
            cts = new();

            CancellationToken token = cts.Token;
            Client.StartReceiving(
                Update,
                BotHandler,
                null,
                token);
        }
        private static void BotHandler(ITelegramBotClient client, Exception ex, CancellationToken token)
        {
            if (ex.Message.Equals("Not found", StringComparison.OrdinalIgnoreCase))
            {
                Log.Create("Bot token not found.", ConsoleColor.Red);
                Break();

                return;
            }
            
            Log.Create($"{nameof(BotHandler)} exception -> {ex.Message}", ConsoleColor.Red);
        }
        private static void Update(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (update.Message == null || update.Message.From == null) return;

            Message msg = update.Message;

            if (isLocked || !SpamFilter.CanSend(msg.From.Username))
                return;

            MessageRouter.RoutMessage(msg);
        }
    }
}
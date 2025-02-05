using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using ImitationOfLife.Commands;

namespace ImitationOfLife
{
    public static class MessageRouter
    {
        private static int messageCount = 0;
        public static int MessageCount => messageCount;
        public static async void RoutMessage(Message message)
        {
            messageCount++;
            try
            {
                switch (message.Type)
                {
                    case MessageType.Unknown:
                        await Client.SendTextMessageAsync(message.Chat.Id, "И что это за сообщение?");
                        break;

                    case MessageType.Text:
                        await HandleText(message);
                        break;

                    case MessageType.Document:
                        await HandleDocument(message);
                        break;

                    case MessageType.Sticker:
                        await HandleSticker(message);
                        break;

                    case MessageType.Dice:
                        await HandleDice(message);
                        break;

                    case MessageType.Photo:
                        await HandlePhoto(message);
                        break;
                    case MessageType.Video:
                        await HandleVideo(message);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                string initiator = nameof(RoutMessage) + " method";

                Log.Create($"{initiator} exception -> " + ex.Message + $"({ex.StackTrace})", ConsoleColor.Red);
                await Client.SendTextMessageAsync(message.Chat.Id, $"{initiator} exception -> {ex.Message}");
                await Client.SendStickerAsync(message.Chat.Id, InputFile.FromFileId("CAACAgIAAxkBAAKCBWZsWZY5oQmo0gSj1SmS-WypbY0CAAKZTwACJuA4S7IK6Oqi4AxFNQQ"));
            }
        }

        private static async Task HandleText(Message message)
        {
            if (message.Text != null)
            {
                Log.FromMessage(message, $"\"{message.Text}\"");

                if (await MixedHandler.ExtractCommand(message))
                    return;

                await TextHandler.ExtractCommand(message);
            }
        }
        private static async Task HandleDocument(Message message)
        {
            if (message.Document == null || message.Document.FileSize == null) return;

            Document document = message.Document;

            if (message.Caption != null)
            {
                if (await MixedHandler.ExtractCommand(message))
                    return;

                Log.FromMessage(message, $"\"{message.Caption}\" [{document.FileName}] ({FileHandler.ConvertSize((long)document.FileSize)})");
                await FileHandler.ExtractCommand(message, document);
            }
            else
            {
                Log.FromMessage(message, $"{document.FileName} ({FileHandler.ConvertSize((long)document.FileSize)})");
                string info = GetBasicInfo(document) +
                    $"| MIME тип: _{document.MimeType}_";
                
                await Client.SendTextMessageAsync(message.Chat.Id, info, parseMode: ParseMode.Markdown);
            }
        }
        private static async Task HandleSticker(Message message)
        {
            if (message.Sticker == null) return;

            Log.FromMessage(message, $"Sticker ({message.Sticker.FileUniqueId})");

            if (message.Sticker.FileUniqueId == "AgAD9EQAAiM14Eo")
                await Client.SendStickerAsync(message.Chat.Id, InputFile.FromFileId("CAACAgIAAxkBAAIf7mXo8Gy_9IY06mQJX7jkFZIVx28XAAL0RAACIzXgSsRXoaZZaRjrNAQ"));
            else
            {
                Sticker sticker = message.Sticker;

                string info = 
                    GetBasicInfo(sticker) +
                    $"| Разрешение: _{sticker.Width}x{sticker.Height}_\n" +
                    $"| Анимированный стикер: _{sticker.IsAnimated}_\n" +
                    $"| Видеостикер: _{sticker.IsVideo}_\n";

                await Client.SendTextMessageAsync(message.Chat.Id, info, parseMode: ParseMode.Markdown);
            }
        }
        private static async Task HandleDice(Message message)
        {
            if (message.Dice == null) return;

            Log.FromMessage(message, $"Dice ({message.Dice.Value})");
            await Client.SendTextMessageAsync(message.Chat.Id, message.Dice.Value.ToString());
        }
        private static async Task HandlePhoto(Message message)
        {
            if(message.Photo == null) return;

            var fileId = message.Photo.Last().FileId;
            
            Log.FromMessage(message, $"Photo ({FileHandler.ConvertSize(message.Photo.Last().FileSize)})");

            await Client.SendTextMessageAsync(message.Chat.Id, GetBasicInfo(await Client.GetFileAsync(fileId)), parseMode: ParseMode.Markdown);
        }
        private static async Task HandleVideo(Message message)
        {
            if(message.Video == null) return;
            Video video = message.Video;

            Log.FromMessage(message, $"Video ({FileHandler.ConvertSize(video.FileSize)})");

            string info = 
                GetBasicInfo(video) + 
                $"| Имя файла: {video.FileName}\n" +
                $"| MIME тип: {video.MimeType}\n" +
                $"| Длительность: {video.Duration}s\n" +
                $"| Разрешение: {video.Width}х{video.Height}\n";

            await Client.SendTextMessageAsync(message.Chat.Id, info, parseMode: ParseMode.Markdown);
        }
        public static string GetBasicInfo(FileBase fb) => 
            $"| Id файла: `{fb.FileId}`\n" +
            $"| Уникальный id: `{fb.FileUniqueId}`\n" +
            $"| Размер: _{FileHandler.ConvertSize(fb.FileSize)}_\n";
    }
    public static class CommandParser
    {
        public const char SplitChar = '<';
        public static string[] SplitMessage(string msg, bool toLower = true)
        {
            if (!msg.Contains(SplitChar))
                return [msg];

            string[] args = msg.Split(SplitChar);
            for (int i = 0; i < args.Length; i++)
            {
                if (string.IsNullOrEmpty(args[i]))
                    break;
                string arg = args[i].Trim();

                if(toLower)
                    args[i] = arg.ToLower();
                else
                    args[i] = arg;
            }
            return args;
        }
        public static string GetKeyword(string msg) => SplitMessage(msg)[0];
        public static string GetArg(string msg)
        {
            int index = msg.IndexOf(SplitChar);

            if(index++ == -1) return "";

            return msg[index..].Trim();
        }

        public static bool TryGetArgs(string msg, out string[] args)
        {
            args = GetArgs(msg);

            if (args.Length < 1) return false;

            return true;
        }
        public static string[] GetArgs(string msg, bool toLower = true)
        {
            if (string.IsNullOrEmpty(msg))
                return [];

            return SplitMessage(msg, toLower).Skip(1).ToArray();
        }
        public static float Clamp(float value, float min, float max) =>
               (value < min) ? min : (value > max ? max : value);

        public static string FindClosestWord(string input, string[] candidates)
        {
            static int GetDistance(string s, string t)
            {
                int n = s.Length;
                int m = t.Length;
                int[,] d = new int[n + 1, m + 1];

                if (n == 0) return m;
                if (m == 0) return n;

                for (int i = 0; i <= n; i++) d[i, 0] = i;
                for (int j = 0; j <= m; j++) d[0, j] = j;

                for (int i = 1; i <= n; i++)
                {
                    for (int j = 1; j <= m; j++)
                    {
                        int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                        d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                    }
                }

                return d[n, m];
            }

            string closest = null;
            int minDistance = int.MaxValue;

            foreach (var candidate in candidates)
            {
                int distance = GetDistance(input, candidate);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = candidate;
                }
            }

            return closest;
        }

        public static bool HasKeyword(ICommand input, string keyword) => keyword.Contains(input.Keyword, StringComparison.OrdinalIgnoreCase);
    }
}
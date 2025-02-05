using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static ImitationOfLife.CommandParser;
using static ImitationOfLife.Commands.FileHandler;

namespace ImitationOfLife.Commands
{
    public static class MixedHandler
    {
        public class Commands
        {
            private readonly static ICommand[] commandList;
            public static ICommand[] CommandList => commandList;
            static Commands()
            {
                var commandTypes = typeof(Commands).GetNestedTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(ICommand)));

                commandList = commandTypes.Select(t => Activator.CreateInstance(t) as ICommand).ToArray();
            }

            public class ToUpper : ITextCommand, IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string filePath = await GetFile(document);
                    string text = System.IO.File.ReadAllText(filePath);

                    System.IO.File.WriteAllText(filePath, text.ToUpper(), Encoding.UTF8);

                    await SendFileAsync(message, filePath);
                    
                    System.IO.File.Delete(filePath);
                }
                public async Task ExecuteText(Message message) => await Client.SendTextMessageAsync(message.Chat.Id, GetArg(message.Text).ToUpper());

                public string Description => "приводит текст в верхний регистр";
                public string Syntax => Keyword;
                public string Keyword => "uppercase";
                public string UsedFileType => "text";
            }
            public class ToLower : ITextCommand, IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string filePath = await GetFile(document);
                    string text = System.IO.File.ReadAllText(filePath);

                    System.IO.File.WriteAllText(filePath, text.ToLower(), Encoding.UTF8);

                    await SendFileAsync(message, filePath);

                    System.IO.File.Delete(filePath);
                }
                public async Task ExecuteText(Message message) => await Client.SendTextMessageAsync(message.Chat.Id, GetArg(message.Text).ToLower());

                public string Description => "приводит текст в нижний регистр";
                public string Syntax => Keyword;
                public string Keyword => "lowercase";
                public string UsedFileType => "text";
            }
            public class SnakeCase : ITextCommand, IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string filepath = await GetFile(document);
                    string contents = ToSnakeCase(System.IO.File.ReadAllText(filepath));

                    System.IO.File.WriteAllText(filepath, contents);

                    await SendFileAsync(message, filepath);

                    System.IO.File.Delete(filepath);
                }
                public async Task ExecuteText(Message message)
                {
                    await Client.SendTextMessageAsync(message.Chat.Id, ToSnakeCase(GetArg(message.Text)));
                }

                public static string ToSnakeCase(string input)
                {
                    return new string(input.Select(c =>
                    {
                        if (c == ' ')
                            return '_';

                        return c;
                    }).ToArray());
                }

                public string Keyword => "snakecase";
                public string Syntax => Keyword;
                public string Description => "заменяет пробелы символом нижнего подчеркивания ('_')";
                public string UsedFileType => "text";
            }
            public class CamelCase : ITextCommand, IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string filepath = await GetFile(document);
                    string contents = ToCamelCase(System.IO.File.ReadAllText(filepath));

                    System.IO.File.WriteAllText(filepath, contents);

                    await SendFileAsync(message, filepath);

                    System.IO.File.Delete(filepath);
                }
                public async Task ExecuteText(Message message)
                {
                    await Client.SendTextMessageAsync(message.Chat.Id, ToCamelCase(GetArg(message.Text)));
                }

                public static string ToCamelCase(string input)
                {
                    StringBuilder sb = new();

                    bool nextUpper = false;

                    foreach (char c in input)
                    {
                        if (c == ' ')
                        {
                            nextUpper = true;
                        }
                        else
                        {
                            if (nextUpper)
                            {
                                sb.Append(char.ToUpper(c));
                                nextUpper = false;
                            }
                            else
                            {
                                sb.Append(char.ToLower(c));
                            }
                        }
                    }

                    return sb.ToString();
                }

                public string Keyword => "camelcase";
                public string Syntax => Keyword;
                public string Description => "убирает пробелы и приводит первую букву каждого слова в верхний регистр";
                public string UsedFileType => "text";
            }
            public class Reverse : ITextCommand, IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string filepath = await GetFile(document);
                    string contents = ToReversed(System.IO.File.ReadAllText(filepath));

                    System.IO.File.WriteAllText(filepath, contents);

                    await SendFileAsync(message, filepath);

                    System.IO.File.Delete(filepath);
                }
                public async Task ExecuteText(Message message)
                {
                    await Client.SendTextMessageAsync(message.Chat.Id, ToReversed(GetArg(message.Text)));
                }

                public static string ToReversed(string input) => new(input.Reverse().ToArray());

                public string Keyword => "reverse";
                public string Syntax => Keyword;
                public string Description => "инвертирует строку";
                public string UsedFileType => "text";
            }
            public class Split : ITextCommand, IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string filepath = await GetFile(document);
                    string[] args = GetArgs(message.Caption);
                    if (args.Length < 1)
                    {
                        await Client.SendTextMessageAsync(message.Chat.Id, "Недостаточно параметров");
                        return;
                    }

                    string splitter = GetArg(message.Caption);
                    string contents = ToSplitted(System.IO.File.ReadAllText(filepath), splitter);

                    System.IO.File.WriteAllText(filepath, contents);

                    await SendFileAsync(message, filepath);

                    System.IO.File.Delete(filepath);
                }
                public async Task ExecuteText(Message message)
                {
                    string[] args = GetArgs(message.Text);
                    if (args.Length < 2)
                        await Client.SendTextMessageAsync(message.Chat.Id, "Недостаточно параметров");

                    string splitter = GetSplitter(GetArg(message.Text));

                    await Client.SendTextMessageAsync(message.Chat.Id, ToSplitted(args[1], splitter));
                }
                public static string GetSplitter(string input) 
                {
                    if (input.Contains('"'))
                    {
                        StringBuilder sb = new();
                        int index = 0;

                        while (input[index] != '"')
                        {
                            index++;
                        }
                        index++;

                        while (input[index] != '"')
                        {
                            sb.Append(input[index++]);
                        }
                        return sb.ToString();
                    }
                    else return input;
                }

                public static string ToSplitted(string input, string splitparam)
                {
                    StringBuilder sb = new();
                    string[] splittedInput = splitparam.Length > 1 ? input.Split(splitparam) : input.Split(splitparam.ToCharArray().First());

                    foreach (string s in splittedInput)
                        sb.AppendLine(s.Trim());

                    return sb.ToString();
                }

                public string Keyword => "split";
                public string Syntax => $"{Keyword} < splitter < string";
                public string Description => "разделяет строку или текст в файле с помощью splitter. Если использовать с текстовым файлом - второй параметр не нужен. При передаче splitter можно для удобства использовать кавычки (\"\"). Все части строки будут написаны в столбик!";
                public string UsedFileType => "text";
            }
            public class SwitchLayout : ITextCommand, IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string filePath = await GetFile(document);

                    string fileContents = System.IO.File.ReadAllText(filePath);

                    if (!TryGetArgs(message.Caption, out var args))
                        return;

                    string langArg = args[0];
                    string output = Switch(fileContents, langArg);

                    if (fileContents == output)
                    {
                        await Client.SendTextMessageAsync(message.Chat.Id, "Не удалось распознать параметр языка");
                        return;
                    }

                    System.IO.File.WriteAllText(filePath, output);

                    await SendFileAsync(message, filePath);

                    System.IO.File.Delete(filePath);
                }

                public async Task ExecuteText(Message message)
                {
                    string[] args = GetArgs(message.Text, false);

                    if (args.Length < 2)
                    {
                        await Client.SendTextMessageAsync(message.Chat.Id, "Недостаточно параметров!");
                        return;
                    }
                    string textArg = args[1];
                    string langArg = args[0].ToLower();

                    string output = Switch(textArg, langArg);

                    if (textArg == output)
                    {
                        await Client.SendTextMessageAsync(message.Chat.Id, "Не удалось распознать параметр языка");
                        return;
                    }

                    await Client.SendTextMessageAsync(message.Chat.Id, output);
                }

                static readonly (char Russian, char English)[] keyMappings =
                {
                    ('й', 'q'), ('Й', 'Q'),
                    ('ц', 'w'), ('Ц', 'W'),
                    ('у', 'e'), ('У', 'E'),
                    ('к', 'r'), ('К', 'R'),
                    ('е', 't'), ('Е', 'T'),
                    ('н', 'y'), ('Н', 'Y'),
                    ('г', 'u'), ('Г', 'U'),
                    ('ш', 'i'), ('Ш', 'I'),
                    ('щ', 'o'), ('Щ', 'O'),
                    ('з', 'p'), ('З', 'P'),
                    ('х', '['), ('Х', '{'),
                    ('ъ', ']'), ('Ъ', '}'),
                    ('ф', 'a'), ('Ф', 'A'),
                    ('ы', 's'), ('Ы', 'S'),
                    ('в', 'd'), ('В', 'D'),
                    ('а', 'f'), ('А', 'F'),
                    ('п', 'g'), ('П', 'G'),
                    ('р', 'h'), ('Р', 'H'),
                    ('о', 'j'), ('О', 'J'),
                    ('л', 'k'), ('Л', 'K'),
                    ('д', 'l'), ('Д', 'L'),
                    ('ж', ';'), ('Ж', ':'),
                    ('э', '\''), ('Э', '"'),
                    ('я', 'z'), ('Я', 'Z'),
                    ('ч', 'x'), ('Ч', 'X'),
                    ('с', 'c'), ('С', 'C'),
                    ('м', 'v'), ('М', 'V'),
                    ('и', 'b'), ('И', 'B'),
                    ('т', 'n'), ('Т', 'N'),
                    ('ь', 'm'), ('Ь', 'M'),
                    ('б', ','), ('Б', '<'),
                    ('ю', '.'), ('Ю', '>'),
                    ('.', '/'), (',', '?'),
                    ('1', '1'), ('!', '!'),
                    ('2', '2'), ('"', '@'),
                    ('3', '3'), ('№', '#'),
                    ('4', '4'), (';', '$'),
                    ('5', '5'), ('%', '%'),
                    ('6', '6'), (':', '^'),
                    ('7', '7'), ('?', '&'),
                    ('8', '8'), ('*', '*'),
                    ('9', '9'), ('(', '('),
                    ('0', '0'), (')', ')'),
                    ('-', '-'), ('_', '_'),
                    ('=', '='), ('+', '+')
                };
                public static string Switch(string input, string language)
                {
                    StringBuilder sb = new();
                    Dictionary<char, char> map;

                    if (language == "ru")
                        map = keyMappings.ToDictionary(k => k.English, v => v.Russian);
                    else if (language == "en")
                        map = keyMappings.ToDictionary(k => k.Russian, v => v.English);
                    else return input;

                    foreach (char c in input)
                    {
                        if(map.TryGetValue(c, out var value))
                            sb.Append(value);
                        else sb.Append(c);
                    }

                    return sb.ToString();
                }

                public string Keyword => "keyboard";
                public string Syntax => $"{Keyword} < ru/en < text";
                public string Description => "меняет буквы на противоположную раскладку, например: йцукен -> qwerty. Парамтр text только для текстовых команд";
                public string UsedFileType => "text";
            }
        }

        public static async Task<bool> ExtractCommand(Message message)
        {
            if(message.Type == MessageType.Text)
            {
                if (message.Text == null)
                    return false;

                string keyword = GetKeyword(message.Text);

                foreach (var item in Commands.CommandList)
                {
                    if (HasKeyword(item, keyword) && item is ITextCommand textItem)
                    {
                        await textItem.ExecuteText(message);
                        return true;
                    }
                }
            }
            else if(message.Type == MessageType.Document)
            {
                Document document = message.Document;
                if(document == null || string.IsNullOrEmpty(message.Caption))
                    return false;

                string errorMessage = ValidateDocument(message, document);

                if (errorMessage != null)
                {
                    await Client.SendTextMessageAsync(message.Chat.Id, errorMessage);
                    return true;
                }

                string keyword = GetKeyword(message.Caption);
                string fileType = GetFileType(document);

                foreach (var item in Commands.CommandList)
                {
                    if(item is IFileCommand fileItem)
                    {
                        bool typeMatching = (fileType == fileItem.UsedFileType) || (fileItem.UsedFileType == "any");

                        if (HasKeyword(item, keyword) && typeMatching)
                        {
                            await fileItem.ExecuteFile(message, document);
                            break;
                        }
                    }
                }
            }

            return false;
        }
    }
}
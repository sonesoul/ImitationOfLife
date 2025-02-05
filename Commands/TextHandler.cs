using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Text;
using static ImitationOfLife.CommandParser;
using System.Data;
using System.Reflection;

namespace ImitationOfLife.Commands
{
    public static class TextHandler
    {
        public static class Commands
        {
            private readonly static ITextCommand[] commandList;
            public static ITextCommand[] CommandList => commandList;
            static Commands()
            {
                var commandTypes = typeof(Commands).GetNestedTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(ITextCommand)));

                commandList = commandTypes.Select(t => Activator.CreateInstance(t) as ITextCommand).ToArray();
            }

            public class Help : ITextCommand
            {
                public async Task ExecuteText(Message message)
                {
                    ChatId chatId = message.Chat.Id;

                    if (!TryGetArgs(message.Text, out var args))
                        await GeneralHelp(chatId);
                    else if (args[0] == "list")
                        await CommandList(chatId);
                    else
                        await CommandHelp(chatId, args[0]);
                }

                public static async Task GeneralHelp(ChatId chatId)
                {
                    string generalHelp =
                       "Привет! Я бот, который работает с помощью команд. \n" +
                       "Команды выполняют разные действия, но пишутся почти одинаково: " +
                       "Текстовые команды работают если отправлять их как текстовое сообщение а Файловые команды работают если сама команда будет в описании к файлу. " +
                       "Я понимаю команды такого формата: \n" +

                       "```\n" +
                       "command < arg1\n\n" +
                       "```\n" +

                       "Все команды делятся на *ключевые слова* и *аргументы*. В примере выше: command - ключевое слово (команда) а \n" +
                       "arg1 - аргумент (параметр команды)\n" +

                       "Твоей первой командой наверное будет: `help < list`, отправь ее мне и я скажу все доступные на данный момент команды! " +
                       "Чтобы узнать подробнее о какой то конкретной отправь: `help < имя команды` (например: `help < resize` или `help < yippee`)" +
                       "В самом списке будут встречаться несколько условных обозначений - например, иногда аргументы бывают необязательными и выглядит это вот так:\n" +

                       "```\n" +
                       "| command (< arg1)" +
                       "```\n" +

                       "в этом примере arg1 можно не вводить (но это влияет на результат!).\n" +
                       "Иногда же нужно выбрать один вариант из нескольких:\n" +

                       "```\n" +
                       "| command < arg1/arg2\n" +
                       "```\n" +

                       "в примере выше обозначено, что команда будет работать если будет одной из этих двух:\n" +

                       "```\n" +
                       "command < arg1\n" +
                       "command < arg2\n" +
                       "```\n" +
                       "Также, команды бывают *смешанными* - это значит что команда может работать например как с файлом так и с текстом. В списке будут обозначения таких команд:\n" +
                       "```\n" +
                       "| command < arg : file, text" +
                       "```\n" +
                       "В данном случае это значит то что команда может работать как Текстовая и как Файловая.\n\n" +

                       $"Вот и всё! Надеюсь, буду полезен^^\n" +
                       $"Чтобы тебе было проще понять принцип, вот несколько примеров команд, которые сработают: \n" +
                       "`random < 10` - сгенерирует число от 0 до 10 (не включая 10)\n" +
                       "`hardchoice < чай или кофе` - поможет выбрать!\n" +
                       "`math < 2 + 2 * 2` - решит пример\n" +
                       "`help < convert` - поможет разобраться в команде\n" +
                       "`llamaai < привет!` - запрос к нейросети \"Ollama 3.1\", вместо \"привет!\" можешь вводить что угодно!\n";

                    await Client.SendTextMessageAsync(chatId, generalHelp, parseMode: ParseMode.Markdown);
                }
                public static async Task CommandHelp(ChatId chatId, string argument)
                {
                    ICommand[] allCommands = [.. commandList, .. FileHandler.Commands.CommandList, .. MixedHandler.Commands.CommandList];

                    for (int i = 0; i < allCommands.Length; i++)
                    {
                        if (argument == allCommands[i].Keyword)
                        {
                            ICommand command = allCommands[i];
                            await Client.SendTextMessageAsync(chatId, $"`{command.Syntax}` _- {command.Description}_", parseMode: ParseMode.Markdown);
                            break;
                        }
                    }
                }
                public static async Task CommandList(ChatId chatId)
                {
                    ICommand[] textCommands = commandList;
                    ICommand[] fileCommands = FileHandler.Commands.CommandList;
                    ICommand[] mixedCommands = MixedHandler.Commands.CommandList;

                    StringBuilder sb = new();

                    sb.AppendLine("*---Текстовые---*");
                    foreach (ICommand command in textCommands)
                    {
                        if (command != null)
                            sb.AppendLine($"| `{command.Syntax}`");
                    }

                    sb.AppendLine("\n*---Файловые---*");
                    foreach (ICommand command in fileCommands)
                    {
                        if (command != null)
                            sb.AppendLine($"| `{command.Syntax}`");
                    }

                    sb.AppendLine("\n*---Смешанные---*");
                    foreach (ICommand command in mixedCommands)
                    {
                        string types = ": ";
                        bool firstParam = true;

                        if (command is ITextCommand)
                        {
                            types += firstParam ? "text" : ", text";
                            firstParam = false;
                        }
                        if (command is IFileCommand)
                        {
                            types += firstParam ? "file" : ", file";
                            firstParam = false;
                        }

                        if (command != null)
                            sb.AppendLine($"| `{command.Syntax}` {types}");
                    }

                    await Client.SendTextMessageAsync(chatId, sb.ToString(), parseMode: ParseMode.Markdown);
                }

                public string Description => $"общая помощь по боту. При передаче list выводит список всех команд а при передаче command выводит описание конкретной команды из списка";
                public string Syntax => $"{Keyword} (< command/list)";
                public string Keyword => "help";
            }
            public class BotRandom : ITextCommand
            {
                public async Task ExecuteText(Message message)
                {
                    if (message.Text == null) return;

                    var chatId = message.Chat.Id;
                    int maxValueArg, minValueArg;
                    string[] args = GetArgs(message.Text);

                    Random rnd = new Random();
                    switch (args.Length)
                    {
                        case 0:

                            await Client.SendDiceAsync(chatId);

                            break;
                        case 1:

                            maxValueArg = int.Parse(args[0]);
                            await Client.SendTextMessageAsync(chatId, rnd.Next(maxValueArg).ToString());

                            break;
                        case 2:

                            minValueArg = int.Parse(args[0]);
                            maxValueArg = int.Parse(args[1]);
                            await Client.SendTextMessageAsync(chatId, rnd.Next(minValueArg, maxValueArg).ToString());

                            break;
                        default:
                            break;
                    }
                }

                public string Keyword => "random";
                public string Syntax => "random (< x (< y))";
                public string Description => "генерирует случайное число от 0 до x или от x до y. Если ничего не передавать - обычный кубик в Telegram";
            }
            public class D20 : ITextCommand
            {
                public async Task ExecuteText(Message message)
                {
                    Random random = new();
                    int result = random.Next(1, 21);

                    string[] phrases =
                    [
                        $"У тебя выпало... Секунду... {result}!",
                        $"Тебе явно не хотелось увидеть {result}, но это твой результат.",
                        $"Кубик выдал {result} целых {random.Next(5, 15)} бросков подряд! Может, это судьба?",
                        $"{result}, кто бы мог подумать?",
                        $"Судьба велела кубику выдать {result}!",
                        $"Говорят, {result} - счастливое число, но не в твоей ситуации ;)",
                        $"И кубик показал {result}. Всё, теперь иди в казино!",
                        $"Поскольку число генерируется псевдо-случайным образом, я сознательно выбираю для тебя {result}!"
                    ];

                    string msg;
                    if (result == 20)
                        msg = $"Cегодня удача явно на твоей стороне потому что твой результат: {result}!";
                    else if (result == 1)
                        msg = $"На кубике виднеется {result}, мир и вправду жесток!";
                    else msg = phrases[new Random().Next(phrases.Length)];

                    await Client.SendTextMessageAsync(message.Chat.Id, msg);
                }

                public string Keyword => "d20";
                public string Syntax => Keyword;
                public string Description => "бросает 20-гранный кубик!";
            }
            public class Yippee : ITextCommand
            {
                public async Task ExecuteText(Message message) =>
                    await Client.SendStickerAsync(
                        message.Chat.Id,
                        InputFile.FromFileId("CAACAgIAAxkBAAIf7mXo8Gy_9IY06mQJX7jkFZIVx28XAAL0RAACIzXgSsRXoaZZaRjrNAQ"));

                public virtual string Keyword => "yippee";
                public string Syntax => Keyword;
                public string Description => "стикер, выражающий высшую форму счастья!";
            }
            public class HardChoice : ITextCommand
            {
                public async Task ExecuteText(Message message)
                {
                    await Task.Delay(0);

                    var args = GetArgs(message.Text);
                    if (args.Length < 1) return;

                    string[] choiceArgs;

                    if (args.Length > 1)
                        choiceArgs = args;
                    else
                    {
                        string splitter = ",";

                        if (args[0].Contains(" or "))
                            splitter = " or ";
                        else if (args[0].Contains(" или "))
                            splitter = " или ";

                        choiceArgs = args[0].Split(splitter).Select(x => x.Trim()).ToArray();
                    }


                    Random random = new();
                    string selectedOption = choiceArgs[random.Next(0, choiceArgs.Length)].Trim().TrimEnd('?');

                    string[] choseStrings =
                    [
                        $"Думаешь, я выберу что то кроме *{selectedOption}*? ",
                        $"Бот задумался... барабанная дробь... и твой выбор: *{selectedOption}*!",
                        $"Мастер-джедай Йода однажды сказал: 'Ваш выбор - *{selectedOption}*, и да пребудет с вами сила!'",
                        $"Итак, после долгих раздумий и магических ритуалов, я выбрал: *{selectedOption}*. А вы думали, я скажу что-то другое?",
                        $"Выбор сделан! Ваш счастливый ответ: *{selectedOption}*!",
                        $"Вы дали мне сложное задание, но я справился! Вот ваш ответ: *{selectedOption}*!",
                        $"Решение принято! Ваш выбор: *{selectedOption}*.",
                        $"Моя интуиция подсказывает, что тебе нужно выбрать *{selectedOption}*!",
                        $"Вот и результаты! Выбранный вариант: *{selectedOption}*!",
                        $"*{selectedOption}* - звучит ужасно, но я выберу это тебе на зло!",
                        $"У тебя сегодня счастливый день, потому что я выбираю: *{selectedOption}*!",
                        $"Судьба выбрала для тебя *{selectedOption}*.",
                        $"Мое двоичное сердце бьется за *{selectedOption}*!",
                        $"Великий алгоритмический советник гласит, что выбор: *{selectedOption}*!",
                        $"Случайность и мудрость объединились, чтобы подсказать мне, что *{selectedOption}* - это ответ на все твои вопросы.",
                        $"Что насчёт *{selectedOption}*?",
                        $"По результатам публичных тестов и опросов, 64.9% выбрали *{selectedOption}*!",
                        $"Я искал звезды... Они сказали мне, что *{selectedOption}* - это правильный выбор.",
                        $"Много людей спорят о том, сколько же будет 2 + 2: то ли 4, то ли 5... Но я считаю что правильный ответ: *{selectedOption}*!"
                    ];

                    async void SendResult()
                    {
                        var msg = await Client.SendTextMessageAsync(message.Chat.Id, "...");
                        int cycleCount = random.Next(6, 8);
                        string[] patterns = ["...", "{0}..", ".{0}.", "..{0}"];
                        ChatId chatId = message.Chat.Id;
                        int messageId = msg.MessageId;


                        for (int i = 0, j = 1; i < cycleCount; i++)
                        {

                            string text = string.Format(patterns[j++], random.Next(2));
                            await Client.EditMessageTextAsync(chatId, messageId, text);
                            await Task.Delay(500);

                            if (j >= patterns.Length)
                                j = 0;
                        }

                        int index = random.Next(choseStrings.Length);
                        await Client.EditMessageTextAsync(message.Chat.Id, msg.MessageId, choseStrings[index], parseMode: ParseMode.Markdown);
                    }

                    SendResult();
                }

                public string Keyword => "hardchoice";
                public string Syntax => "hardchoice < вариант1 </,/или/or вариант2...";
                public string Description => "помогает в ситуациях когда сложно выбрать! Принимает любое количество вариантов";
            }
            public class EightBall : ITextCommand
            {
                public async Task ExecuteText(Message message)
                {
                    string[] ballAnswers =
                    {
                        "Да, определённо",
                        "Без сомнений",
                        "Абсолютно да",
                        "Вероятнее всего",
                        "Знаки говорят 'да'",
                        "Да",
                        "Скорее всего",
                        "Перспективы хорошие",
                        "Да, но будь осторожен",
                        "Ответ туманен, попробуй снова",
                        "Спроси позже",
                        "Лучше не говорить сейчас",
                        "Сейчас нельзя предсказать",
                        "Сконцентрируйся и спроси снова",
                        "Не рассчитывай на это",
                        "Мой ответ - нет",
                        "Мои источники говорят 'нет'",
                        "Перспективы не очень",
                        "Очень сомнительно",
                        "Вряд ли"
                    };

                    await Client.SendTextMessageAsync(message.Chat.Id, ballAnswers[new Random().Next(20)]);
                }

                public string Keyword => "8ball";
                public string Syntax => Keyword;
                public string Description => "магический шар-восьмёрка!";
            }
            public class FlipCoin : ITextCommand
            {
                public async Task ExecuteText(Message message) => await Client.SendTextMessageAsync(message.Chat.Id, new Random().Next(2) == 0 ? $"Орел!" : "Решка!");

                public string Keyword => "flipcoin";
                public string Syntax => Keyword;
                public string Description => "бросает монетку, ничего более!";
            }
            public class BotConvert : ITextCommand
            {
                private static readonly Dictionary<string, double> sizeUnits = new()
                {
                    { "b", 1.0 / 1024 / 1024},
                    { "kb", 1.0 / 1024 },
                    { "mb", 1 },
                    { "gb", 1024 },
                    { "tb", 1024L * 1024 }
                };
                private static readonly Dictionary<string, double> distanceUnits = new()
                {
                    { "mm", 0.1},
                    { "cm", 1},
                    { "m", 100},
                    { "km", 1000},
                };
                private static readonly Dictionary<string, double> massUnits = new()
                {
                    { "g", 0.01},
                    { "kg", 1 },
                    { "t", 1000 }
                };
                private static readonly Dictionary<string, double> timeUnits = new()
                {
                    { "ms", 0.01 },
                    { "sec", 1},
                    { "min", 60},
                    { "h", 3600},
                    { "d", 86400 }
                };
                private static readonly Dictionary<string, double> angleUnits = new()
                {
                    { "rad", 1 },
                    { "deg", Math.PI / 180 }
                };
                private static readonly Dictionary<string, double> liquidUnits = new()
                {
                    { "ml", 0.001 },
                    { "l", 1 }
                };

                public async Task ExecuteText(Message message)
                {
                    string[] args = GetArgs(message.Text);
                    string[] firstArgPair = args[0].Split(' ');
                    ChatId chatId = message.Chat.Id;

                    if (args.Length < 2 || firstArgPair.Length < 2)
                    {
                        await Client.SendTextMessageAsync(chatId, "Недостаточно параметров");
                        return;
                    }
                    if (!double.TryParse(firstArgPair[0], out var value))
                    {
                        await Client.SendTextMessageAsync(chatId, "Недопустимое значение для value");
                        return;
                    }

                    string fromUnit = firstArgPair[1];
                    string toUnit = args[1];
                    var fromUnitCollection = GetCollection(fromUnit);
                    var toUnitCollection = GetCollection(toUnit);

                    if (fromUnitCollection == null)
                    {
                        await Client.SendTextMessageAsync(chatId, $"Не найдена величина {fromUnit}");
                        return;
                    }
                    if (toUnitCollection == null)
                    {
                        await Client.SendTextMessageAsync(chatId, $"Не найдена величина {toUnit}");
                        return;
                    }

                    double result = ConvertUnits(value, fromUnitCollection[fromUnit], toUnitCollection[toUnit]);
                    await Client.SendTextMessageAsync(message.Chat.Id, $"{value} {fromUnit.ToLower()} = {result} {toUnit.ToLower()}");
                }

                public static Dictionary<string, double> GetCollection(string key)
                {
                    Dictionary<string, double>[] collections =
                    [
                        sizeUnits,
                        distanceUnits,
                        massUnits,
                        liquidUnits,
                        timeUnits,
                        angleUnits,
                    ];

                    foreach (var item in collections)
                    {
                        if (item.ContainsKey(key))
                            return item;
                    }
                    return null;
                }
                public static double ConvertUnits(double value, double from, double to) => value * (from / to);

                public string Description => "конвертирует value между from и to, например: convert < 1024 kb < mb. Поддерживаются такие величины как:\n\n" +
                    "размер файлов (b, kb, mb, gb, tb)\n" +
                    "расстояние (mm, cm, m, km)\n" +
                    "масса (g, kg, t)\n" +
                    "время (ms, sec, min, h, day)\n" +
                    "угол (deg, rad)\n" +
                    "жидкости (ml, l)";
                public string Syntax => $"{Keyword} < value from < to";
                public string Keyword => "convert";
            }
            public class MathClass : ITextCommand
            {
                public readonly struct MethodRequest
                {
                    public readonly string MethodName;
                    public readonly string[] MethodArgs;
                    public readonly string FullRequest;

                    public MethodRequest(string methodName, string[] args, string original)
                    {
                        MethodName = methodName;
                        MethodArgs = args;
                        FullRequest = original;
                    }
                }

                public async Task ExecuteText(Message message)
                {
                    if (message.Text == null) return;
                    
                    string expression = GetArg(message.Text).Replace('\n', ' ');

                    if (expression == "all")
                    {
                        MethodInfo[] allMethods = typeof(Math).GetMethods()
                        .GroupBy(m => m.Name)
                        .Select(g => g.First())
                        .SkipLast(3)
                        .ToArray();

                        StringBuilder sb = new();
                        foreach (var item in allMethods)
                            sb.AppendLine($"{item.Name} < {string.Join(", ", item.GetParameters().Select(p => p.Name).ToArray())}");

                        await Client.SendTextMessageAsync(message.Chat.Id, sb.ToString());
                        return;
                    }

                    string result = ProcessExpression(expression);

                    await Client.SendTextMessageAsync(message.Chat.Id, Calculate(result)?.ToString().Replace(',', '.') ?? "Не удалось посчитать!");
                }

                public static decimal? Calculate(string expression)
                {
                    try
                    {
                        expression = expression.Replace(',', '.');

                        using (DataTable table = new())
                        {
                            table.Columns.Add(new DataColumn("expression", typeof(decimal), expression));
                            table.Rows.Add(0);

                            return (decimal)(table.Rows[0]["expression"]);
                        }
                    }
                    catch
                    {
                        return null;
                    }
                }
                public static string MathInvoke(string methodName, string[] args)
                {
                    MethodInfo[] allMethods = typeof(Math).GetMethods()
                        .Where(m => m.GetParameters().All(p => p.ParameterType == typeof(double)))
                        .GroupBy(m => m.Name)
                        .Select(g => g.First())
                        .SkipLast(3)
                        .ToArray();

                    for (int i = 0; i < args.Length; i++)
                        args[i] = Calculate(args[i]).ToString() ?? args[i];

                    MethodInfo method = allMethods
                        .Where(m => m.Name.ToLower() == methodName.ToLower())
                        .FirstOrDefault(m => m.GetParameters().Length == args.Length);

                    if (method == null)
                        return $"Ошибка при поиске метода ({methodName})";

                    List<object> methodArgs = [];
                    var methodParams = method.GetParameters();
                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        try
                        {
                            methodArgs.Add(Convert.ChangeType(args[i], methodParams[i].ParameterType));
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    return method.Invoke(null, methodArgs.ToArray())?.ToString() ?? "Не удалось получить ответ";
                }

                public static string ProcessExpression(string expression)
                {
                    var mathRequests = GetMethodRequests(expression);

                    foreach (var request in mathRequests)
                    {
                        string[] resolvedArgs = request.MethodArgs.Select(arg => ProcessExpression(arg)).ToArray();
                        string methodResult = MathInvoke(request.MethodName, resolvedArgs);
                        expression = ReplaceFirst(expression, request.FullRequest, methodResult);
                    }

                    return expression;
                }

                public static MethodRequest[] GetMethodRequests(string input)
                {
                    List<MethodRequest> nameArgsPairs = new();

                    string[] parts = GetInsideBrackets(input).Where(t => t != null).ToArray();

                    foreach (string part in parts)
                    {
                        int methodEnd = part.IndexOf('<');
                        if (methodEnd < 0) continue;

                        string methodName = part.Substring(0, methodEnd).Trim();
                        string[] methodArgs = SplitArguments(part.Substring(methodEnd + 1).Trim());

                        nameArgsPairs.Add(new MethodRequest(methodName, methodArgs, part));
                    }

                    return nameArgsPairs.ToArray();
                }
                public static string[] SplitArguments(string input)
                {
                    List<string> args = new();
                    int length = input.Length;
                    int bracketLevel = 0;
                    StringBuilder currentArg = new();

                    for (int i = 0; i < length; i++)
                    {
                        char c = input[i];
                        if (c == '[')
                        {
                            bracketLevel++;
                        }
                        else if (c == ']')
                        {
                            bracketLevel--;
                        }

                        if (c == ',' && bracketLevel == 0)
                        {
                            args.Add(currentArg.ToString().Trim());
                            currentArg.Clear();
                        }
                        else
                        {
                            currentArg.Append(c);
                        }
                    }

                    if (currentArg.Length > 0)
                    {
                        args.Add(currentArg.ToString().Trim());
                    }

                    return args.ToArray();
                }
                public static string[] GetInsideBrackets(string input)
                {
                    List<string> results = new();
                    int length = input.Length;
                    int bracketLevel = 0;
                    int lastStart = -1;

                    for (int i = 0; i < length; i++)
                    {
                        char c = input[i];
                        if (c == '[')
                        {
                            if (bracketLevel == 0)
                            {
                                lastStart = i;
                            }
                            bracketLevel++;
                        }
                        else if (c == ']')
                        {
                            bracketLevel--;
                            if (bracketLevel == 0)
                            {
                                results.Add(input.Substring(lastStart + 1, i - lastStart - 1));
                            }
                        }
                    }

                    return results.ToArray();
                }
                public static string ReplaceFirst(string input, string oldValue, string newValue)
                {
                    int index = input.IndexOf(oldValue);
                    if (index < 0) return input;

                    return input.Substring(0, index -1) + newValue + input.Substring(index + oldValue.Length + 1);
                }

                public string Keyword => "math";
                public string Syntax => $"{Keyword} < expression";
                public string Description =>
                    "достаточно простой калькулятор, из за некоторых ограничений работает с относительно небольшим диапазоном чисел. " +
                    "Чтобы что-то посчитать просто введи выражение как текст, например:\n" +
                    "59 * 10 / 4 + (2 + 2 * 2). " +
                    "Содержит интеграцию класса Math, его использование немного похоже на обычные мои команды, но здесь они называются методами и " +
                    "для передачи нескольких аргументов используется запятая. Выглядит это вот так:\n" +
                    "метод < аргумент1, аргумент2\n" +
                    "все методы можно узнать с помощью math < all, " +
                    "а в выражении они должны быть закрыты в квадратные скобки, например:\n" +
                    "10 + [sin < 10]\n" +
                    "В сами методы тоже можно передавать выражения:\n" +
                    "[sin < 43.9 / 2] - 14\n" +
                    "или даже другие методы:\n" +
                    "[sin < [cos < 40]]";
            }
            public class Shorten : ITextCommand
            {
                public async Task ExecuteText(Message message)
                {
                    if (message.Text == null)
                        return;

                    var arg = GetArg(message.Text);
                    var shortUrl = await ShortenUrlAsyncNetHttp(arg);

                    await Client.SendTextMessageAsync(message.Chat.Id, $"`{shortUrl}`", parseMode: ParseMode.Markdown);
                }
                public static async Task<string> ShortenUrlAsyncNetHttp(string longUrl)
                {
                    using (var httpClient = new HttpClient())
                    {
                        string requestUrl = $"https://tinyurl.com/api-create.php?url={Uri.EscapeDataString(longUrl)}";
                        var response = await httpClient.GetAsync(requestUrl);

                        return await response.Content.ReadAsStringAsync();
                    }
                }

                public string Description => "сокращает ссылки!";
                public string Syntax => $"{Keyword} < url";
                public string Keyword => "shorten";
            }
            public class Throw : ITextCommand
            {
                public async Task ExecuteText(Message message)
                {
                    List<Exception> exceptions = new();

                    Assembly assembly = Assembly.GetAssembly(typeof(Exception));

                    if (assembly == null)
                        return;

                    foreach (var item in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Exception)) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null))
                    {
                        var instance = Activator.CreateInstance(item);

                        if (instance is Exception ex)
                            exceptions.Add(ex);
                    }

                    try
                    {
                        throw exceptions[new Random().Next(exceptions.Count)];
                    }
                    catch (Exception ex)
                    {
                        await Client.SendTextMessageAsync(message.Chat.Id, ex.Message);
                        Log.Create(ex.Message, ConsoleColor.Red);
                    }

                    
                }
                public string Description => "выбрасывает случайное исключение, больше ничего не делает";
                public string Syntax => Keyword;
                public string Keyword => "throw";
            }
            public class GetChatId : ITextCommand
            {
                public async Task ExecuteText(Message message)
                {
                    await Client.SendTextMessageAsync(message.Chat.Id, message.Chat.Id.ToString());
                }

                public string Description => "узнать chat id";
                public string Syntax => Keyword;
                public string Keyword => "chatid";
            }
        }
        public static async Task ExtractCommand(Message message)
        {
            if (message.Text == null)
                return;

            string keyword = GetKeyword(message.Text);

            foreach (var item in Commands.CommandList)
            {
                if (item == null)
                    continue;

                if (HasKeyword(item, keyword))
                {
                    await item.ExecuteText(message);
                    return;
                }
            }

            await Client.SendTextMessageAsync(
                message.Chat.Id,
                $"Хм, кажись такой команды нет. Может, ты имеешь ввиду команду `{FindClosestWord(keyword, Commands.CommandList.Concat(MixedHandler.Commands.CommandList).Select(c => c.Keyword).ToArray())}`?",
                parseMode: ParseMode.Markdown);
        }
    }
}
using Telegram.Bot.Types;
using Telegram.Bot;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System.Text;
using static ImitationOfLife.CommandParser;
using static ImitationOfLife.Config;
using static ImitationOfLife.Commands.FileHandler.Commands;
using Telegram.Bot.Types.Enums;

namespace ImitationOfLife.Commands
{
    public static class FileHandler
    {
        public static class Commands
        {
            private readonly static IFileCommand[] commandList;
            public static IFileCommand[] CommandList => commandList;

            static Commands()
            {
                var commandTypes = typeof(Commands).GetNestedTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IFileCommand)));

                commandList = commandTypes.Select(t => Activator.CreateInstance(t) as IFileCommand).ToArray();
            }

            public class Resize : IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    if (!TryGetArgs(message.Caption, out var args))
                        return;

                    string resArg = args[0];
                    bool saveAspectArg = args.Length > 1 && bool.TryParse(args[1], out var result) ? result : false;
                    if (ImageSize.TryGetSize(resArg, out var targetSize))
                    {
                        string imagePath = await GetFile(document);
                        if (saveAspectArg)
                        {
                            ImageSize originalSize = ImageSize.FromFile(imagePath);
                            originalSize.SaveAspectRatio(targetSize);
                            targetSize = originalSize;
                        }


                        targetSize.Clamp(1, 2048);

                        ImageHelper.Resize(imagePath, targetSize, KnownResamplers.NearestNeighbor);
                        await SendFileAsync(message, imagePath, caption: $"Новое разрешение: {targetSize.Width}x{targetSize.Height}");
                        System.IO.File.Delete(imagePath);
                    }
                }

                public string UsedFileType => "image";
                public string Keyword => "resize";
                public string Syntax => $"{Keyword} < width X height (< true)";
                public string Description => "изменяет размер картинки в пикселях. Если передать true, соотношение сторон сохранится!";
            }
            public class Ascii : IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    if (!TryGetArgs(message.Caption, out var args))
                        return;

                    if (!ImageSize.TryGetSize(args[0], out ImageSize size))
                    {
                        await Client.SendTextMessageAsync(message.Chat.Id, "Недопустимый размер!");
                        return;
                    }

                    size.Clamp(1, 256);
                    size.Height /= 2;
                    string imagePath = await GetFile(document);
                    string artfilePath = Path.Combine(GetTempDirectory(), "artfile.txt");
                    ImageHelper.Resize(imagePath, size);
                    string artStr;

                    using (Image<Rgb24> image = Image.Load<Rgb24>(imagePath))
                    {
                        artStr = CreateAscii(image);
                        System.IO.File.WriteAllText(artfilePath, artStr);
                    }

                    await SendFileAsync(message, artfilePath, $"{Path.GetFileNameWithoutExtension(imagePath)}.txt", $"Символов: {artStr.ToCharArray().Length}\nСтрок: {artStr.Count(c => c == '\n')}");

                    System.IO.File.Delete(imagePath);
                    System.IO.File.Delete(artfilePath);
                }
                public static string CreateAscii(Image<Rgb24> image)
                {
                    StringBuilder sb = new();
                    char[] asciiChars = ['.', ',', '-', '~', ':', ';', '=', '!', '+', '*', '#', '$', '@'];

                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            Rgb24 pixelColor = image[x, y];

                            int brightness = (int)(0.299 * pixelColor.R + 0.587 * pixelColor.G + 0.114 * pixelColor.B);

                            int index = brightness * (asciiChars.Length - 1) / 255;
                            sb.Append(asciiChars[index]);
                        }
                        sb.Append('\n');
                    }

                    return sb.ToString();
                }

                public string UsedFileType => "image";
                public string Keyword => "ascii";
                public string Syntax => $"{Keyword} < rows X columns";
                public string Description => "cоздает ASCII арт из картинки!";
            }
            public class Format : IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    if (!TryGetArgs(message.Caption, out var args))
                        return;

                    string newFormatArg = args[0];
                    IImageEncoder encoder = ImageHelper.GetEncoder(newFormatArg);

                    if (encoder == null)
                    {
                        await Client.SendTextMessageAsync(message.Chat.Id, $"Не могу определить формат: {newFormatArg}");
                        return;
                    }

                    string imagePath = await GetFile(document);

                    if (ImageHelper.FormatEquals(encoder, ImageHelper.FileFormat(imagePath)))
                    {
                        await Client.SendTextMessageAsync(message.Chat.Id, $"Картинка итак в {newFormatArg} формате, изменения не нужны");
                        System.IO.File.Delete(imagePath);
                        return;
                    }

                    ImageHelper.SetFormat(imagePath, encoder);

                    await SendFileAsync(message, imagePath, $"{Path.GetFileNameWithoutExtension(imagePath)}.{newFormatArg}");
                    System.IO.File.Delete(imagePath);
                }
                public string UsedFileType => "image";
                public string Keyword => "format";
                public string Syntax => $"{Keyword} < png/jpg/webp";
                public string Description => "изменяет формат картинки";

            }
            public class ToSticker : IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string imagePath = await GetFile(document);
                    string filenameNoExt = Path.GetFileNameWithoutExtension(imagePath);
                    string[] args = GetArgs(message.Caption);

                    ImageSize size = ImageSize.FromFile(imagePath);

                    bool saveAspect = true;
                    bool changeFormat = true;
                    bool resizeImage = true;

                    if (ImageHelper.IsImageIn<PngFormat>(imagePath))
                    {
                        changeFormat = false;
                    }

                    if (args.Length > 0 && args[0] == "false")
                        saveAspect = false;

                    if ((size.Width == 512 && size.Height <= 512) || (size.Height == 512 && size.Width <= 512))
                    {
                        if (!saveAspect && (size.Width != 512 || size.Height != 512))
                            resizeImage = true;
                        else
                            resizeImage = false;
                    }

                    if (resizeImage)
                    {
                        if (saveAspect)
                            size.SaveAspectRatio(512, 512);
                        else
                            size = new(512, 512);

                        ImageHelper.Resize(imagePath, size);
                    }

                    if (changeFormat)
                    {
                        ImageHelper.SetFormat(imagePath, new PngEncoder());
                    }

                    size = ImageSize.FromFile(imagePath);
                    string caption = "Ничего не изменилось";

                    if (changeFormat || resizeImage)
                    {
                        caption = "Изменено: \n" +
                        (changeFormat ? $"Формат\n" : "") +
                        (resizeImage ? $"Разрешение ({size.Width}x{size.Height})\n" : "");
                    }

                    await SendFileAsync(message, imagePath, filenameNoExt + ".png", caption);

                    System.IO.File.Delete(imagePath);
                }
                public string UsedFileType => "image";
                public string Keyword => "tosticker";
                public string Syntax => $"{Keyword} (< false)";
                public string Description =>
                   $"делает картинку подходящей для стикера в Telegram - изменяет формат на .png и разрешение на 512х512. " +
                   $"Если передать 'false' то соотношение сторон не будет сохранено";
            }
            public class Pixelate : IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string[] args = GetArgs(message.Caption);
                    if (args.Length <= 0 || !int.TryParse(args[0], out int pixelScale)) return;
                    string imagePath = await GetFile(document);

                    pixelScale = (int)Clamp(pixelScale, 1f, 100f);

                    ImageSize imageSize = ImageSize.FromFile(imagePath);

                    ImageHelper.Resize(imagePath, imageSize / pixelScale, KnownResamplers.NearestNeighbor);
                    imageSize = ImageSize.FromFile(imagePath);
                    ImageHelper.Resize(imagePath, imageSize * pixelScale, KnownResamplers.NearestNeighbor);

                    await SendFileAsync(message, imagePath);

                    System.IO.File.Delete(imagePath);
                }

                public string UsedFileType => "image";
                public string Keyword => "pixelate";
                public string Syntax => $"{Keyword} < pixelSize";
                public string Description => "пикселизирует картинку. pixelSize должно быть целым числом (от 1 до 100), чем больше значение тем больше пикселизация!";

            }
            public class Rotate : IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string[] args = GetArgs(message.Caption);

                    if (!int.TryParse(args[0], out int degrees)) return;

                    degrees = (int)Clamp(degrees, -360, 360);

                    string imagePath = await GetFile(document);

                    using (Image image = Image.Load(imagePath))
                    {
                        image.Mutate(x => x.Rotate(degrees, KnownResamplers.NearestNeighbor));
                        image.Save(imagePath);
                    }

                    await SendFileAsync(message, imagePath);
                    System.IO.File.Delete(imagePath);
                }

                public string UsedFileType => "image";
                public string Keyword => "rotate";
                public string Syntax => $"{Keyword} < degrees";
                public string Description => "поворачивает картинку (в градусах)";
            }
            public class Flip : IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string[] args = GetArgs(message.Caption);
                    if (args.Length < 1) return;

                    string fliptype = args[0].Trim().ToLower();

                    FlipMode flipMode;

                    if (fliptype == "x" || fliptype == "horizontal")
                        flipMode = FlipMode.Horizontal;
                    else if (fliptype == "y" || fliptype == "vertical")
                        flipMode = FlipMode.Vertical;
                    else return;


                    string imagePath = await GetFile(document);

                    using (Image image = Image.Load(imagePath))
                    {
                        image.Mutate(x => x.Flip(flipMode));
                        image.Save(imagePath);
                    }

                    await SendFileAsync(message, imagePath);
                    System.IO.File.Delete(imagePath);
                }

                public string UsedFileType => "image";
                public string Keyword => "flip";
                public string Syntax => $"{Keyword} < x/y/vertical/horizontal";
                public string Description => "отзеркаливает картинку (по горизонтали или вертикали)";
            }
            public class Grayscale : IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string imagePath = await GetFile(document);

                    ApplyGrayscale(imagePath);

                    await SendFileAsync(message, imagePath);
                    System.IO.File.Delete(imagePath);
                }
                public static void ApplyGrayscale(string imagePath)
                {
                    using (Image<Rgba32> image = Image.Load<Rgba32>(imagePath))
                    {
                        image.Mutate(ctx => ctx.Grayscale());
                        image.Save(imagePath);
                    }
                }

                public string UsedFileType => "image";
                public string Keyword => "grayscale";
                public string Syntax => $"{Keyword}";
                public string Description => "заменяет цвета в картинке на оттенки серого";
            }
            public class Sketch : IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    string imagePath = await GetFile(document);

                    int blurValue = 10;

                    if (TryGetArgs(message.Caption, out var args) && int.TryParse(args[0], out var parsed))
                    {
                        blurValue = (int)Clamp(parsed, 1, 1000);
                    }

                    using (Image<Rgba32> image = Image.Load<Rgba32>(imagePath))
                    {
                        image.Mutate(ctx => ctx.Grayscale());

                        using (Image<Rgba32> blurredImage = image.Clone(ctx => ctx.GaussianBlur(blurValue)))
                        {
                            ApplySketch(image, blurredImage);
                        }

                        image.Save(imagePath);
                    }

                    await SendFileAsync(message, imagePath);
                    System.IO.File.Delete(imagePath);
                }
                public static void ApplySketch(Image<Rgba32> image, Image<Rgba32> blurredImage)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            var originalPixel = image[x, y];
                            var blurredPixel = blurredImage[x, y];

                            byte r = (byte)(255 - Math.Abs(originalPixel.R - blurredPixel.R));
                            byte g = (byte)(255 - Math.Abs(originalPixel.G - blurredPixel.G));
                            byte b = (byte)(255 - Math.Abs(originalPixel.B - blurredPixel.B));

                            image[x, y] = new Rgba32(r, g, b);
                        }
                    }
                }

                public string UsedFileType => "image";
                public string Keyword => "sketch";
                public string Syntax => $"{Keyword} (< blurValue)";
                public string Description => "накладывает эффект скетча на картинку";
            }
            public class Compress : IFileCommand
            {
                public async Task ExecuteFile(Message message, Document document)
                {
                    if (!TryGetArgs(message.Caption, out string[] args) || 
                        !int.TryParse(args[0], out int percentage)) return;


                    string filePath = await GetFile(document);

                    percentage = (int)Clamp(percentage, 1, 100);

                    ImageSize originalSize = ImageSize.FromFile(filePath);
                    ImageSize compressedSize = (originalSize / 100) * percentage;


                    ImageHelper.Resize(filePath, compressedSize, new TriangleResampler());
                    ImageHelper.Resize(filePath, originalSize, new TriangleResampler());


                    await SendFileAsync(message, filePath);
                    System.IO.File.Delete(filePath);
                }

                public string Keyword => "compress";
                public string Syntax => $"{Keyword} < percentage";
                public string Description => "сжимает картинку. Percentage определяет новое качество относительно текущего (от 1 до 100)";
                public string UsedFileType => "image";
            }

            public class Info : IFileCommand
            {
                public string UsedFileType => "any";
                public string Description => "выводит общую информацию о файле, работает с картинками и текстовыми файлами";
                public string Syntax => Keyword;
                public string Keyword => "info";

                private Dictionary<string, Func<Message, Document, Task>> mimeActionPairs = new()
                {
                    { "image", ImageInfo },
                    { "text", TextInfo },
                };

                public async Task ExecuteFile(Message message, Document document)
                {
                    string mime = GetFileType(document);

                    if (mimeActionPairs.TryGetValue(mime, out var value))
                        await value.Invoke(message, document);
                }

                private static async Task ImageInfo(Message message, Document document)
                {
                    string imagePath = await GetFile(document);

                    string resolution, size, format;

                    using (Image image = Image.Load(imagePath))
                    {
                        resolution = $"{image.Width}x{image.Height}";
                        format = Image.DetectFormat(imagePath).Name.ToLower();
                        size = ConvertSize(document.FileSize != null ? document.FileSize.Value : 0);
                    }
                    string infoText =
                        $"Разрешение: {resolution}\n" +
                        $"Формат: {format}\n" +
                        $"Размер: {size}\n";

                    await Client.SendTextMessageAsync(message.Chat.Id, infoText);

                    System.IO.File.Delete(imagePath);
                }
                private static async Task TextInfo(Message message, Document document)
                {
                    string filePath = await GetFile(document);

                    string fileText = System.IO.File.ReadAllText(filePath);

                    char[] delimiters = [' ', '\r', '\n', '\t', ',', ';', '.', '!', '?'];
                    string[] words = fileText.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    string[] lines = fileText.Split('\n');

                    int charsCount = fileText.Length;
                    int wordsCount = words.Length;
                    int linesCount = lines.Length;

                    string infoText =
                       $"Символов: {charsCount}\n" +
                       $"Слов: {wordsCount}\n" +
                       $"Строк: {linesCount}\n";

                    await Client.SendTextMessageAsync(message.Chat.Id, infoText);

                    System.IO.File.Delete(filePath);
                }
            }
        }

        public readonly static HashSet<string> imageMimes = ["image/png", "image/jpeg", "image/webp"];
        public readonly static HashSet<string> textMimes = ["text/plain"];
        public readonly static HashSet<string> supportedMimes = [.. imageMimes, .. textMimes];
        public const int OneMBInBytes = 1048576;
        public static async Task ExtractCommand(Message message, Document document)
        {
            string errorMessage = ValidateDocument(message, document);

            if (errorMessage != null)
            {
                await Client.SendTextMessageAsync(message.Chat.Id, errorMessage);
                return;
            }

            string keyword = GetKeyword(message.Caption ?? "");
            string fileType = GetFileType(document);

            foreach (var item in CommandList)
            {
                if (item == null)
                    continue;

                bool typeMatching = (fileType == item.UsedFileType) || (item.UsedFileType == "any");

                if (HasKeyword(item, keyword) && typeMatching)
                {
                    await item.ExecuteFile(message, document);
                    return;
                }
            }

            await Client.SendTextMessageAsync(
                message.Chat.Id,
                $"Хм, кажись такой команды нет. Может, ты имеешь ввиду команду `{FindClosestWord(keyword, CommandList.Concat(MixedHandler.Commands.CommandList).Select(c => c.Keyword).ToArray())}`?",
                parseMode: ParseMode.Markdown);
        }
        public static async Task<string> GetFile(Document document)
        {
            var fileId = await Client.GetFileAsync(document.FileId);

            if (document.FileName == null)
                throw new ArgumentNullException(nameof(document), $"FileName is null");
            else if (fileId.FilePath == null)
                throw new ArgumentNullException(nameof(document), $"FilePath is null");

            string downloadPath = GeneratePath(document);

            using (var fs = new FileStream(downloadPath, FileMode.Create))
            {
                await Client.DownloadFileAsync(fileId.FilePath, fs);
            }

            return downloadPath;
        }
        public static async Task SendFileAsync(Message message, string filePath, string filename = null, string caption = null)
        {
            if (message.Document == null)
                return;

            filename ??= message.Document.FileName;
            ChatId id = message.Chat.Id;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {

                var inputFile = InputFile.FromStream(fs, filename);

                if (caption == null)
                    await Client.SendDocumentAsync(id, inputFile);
                else
                    await Client.SendDocumentAsync(id, inputFile, caption: caption);
            }
        }
        public static string ValidateDocument(Message message, Document document)
        {
            if (message.Caption == null)
                return "У файла нет описания";
            if (document.MimeType == null)
                return "Формат файла равен null";
            if (document.FileSize > OneMBInBytes * 10)
                return "Файл превышает максимально допустимый размер";
            if (!supportedMimes.Contains(document.MimeType))
                return "Такой формат не поддерживается";
            return null;
        }

        public static string GeneratePath(Document document)
        {
            if (document.FileName == null)
                throw new ArgumentNullException(nameof(document), $"FileName is null");

            string filename = document.FileName;
            string path = Path.Combine(GetTempDirectory(), filename);

            int nameIndex = 0;
            bool fileExists = System.IO.File.Exists(path);

            while (fileExists)
            {
                path = Path.Combine(GetTempDirectory(), $"{nameIndex++}_{filename}");
                fileExists = System.IO.File.Exists(path);
            }

            return path;
        }
        public static string GetFileType(Document file)
        {
            string mimeType = file.MimeType ?? "";
            return mimeType.Split('/')[0].ToLower();
        }
        public static string ConvertSize(long? sizeInBytes)
        {
            if (sizeInBytes == null)
                return "0 b";

            ulong sizeBytes = (ulong)sizeInBytes;
            double sizeKb = sizeBytes / 1024.0;
            double sizeMb = sizeKb / 1024.0;
            double sizeGb = sizeMb / 1024.0;

            string finalSize;
            if (sizeGb >= 1)
                finalSize = $"{sizeGb:F2} GB";
            else if (sizeMb >= 1)
                finalSize = $"{sizeMb:F2} MB";
            else if (sizeKb >= 1)
                finalSize = $"{sizeKb:F2} KB";
            else
                finalSize = $"{sizeBytes} b";
            return finalSize.ToString();
        }
    }
    public struct ImageSize
    {
        public int Height { get; set; }
        public int Width { get; set; }

        public ImageSize(int width, int height) => SetSize(width, height);
        public ImageSize(Image image) => SetSize(image);
        public ImageSize(string stringRes) => TrySetSize(stringRes);

        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
        public void SetSize(Image image) => SetSize(image.Width, image.Height);
        public bool TrySetSize(string WxH)
        {
            SetSize(1, 1);

            string[] stringSize = WxH.Split('x');
            if (stringSize.Length < 2) return false;

            if (int.TryParse(stringSize[0], out int width) && int.TryParse(stringSize[1], out int height))
            {
                SetSize(width, height);
                return true;
            }

            return false;
        }
        public void SaveAspectRatio(int targetWidth, int targetHeight)
        {
            double aspectRatio = (double)Width / Height;
            if (aspectRatio > 1)
            {
                Width = targetWidth;
                Height = (int)(targetWidth / aspectRatio);
            }
            else
            {
                Width = (int)(targetHeight * aspectRatio);
                Height = targetHeight;
            }
        }
        public void SaveAspectRatio(ImageSize targetSize) => SaveAspectRatio(targetSize.Width, targetSize.Height);

        public void Clamp(float min, float max)
        {
            Width = (int)ClampValue(Width, min, max);
            Height = (int)ClampValue(Height, min, max);
        }

        public static bool TryGetSize(string WxH, out ImageSize resolution)
        {
            resolution = new ImageSize(1, 1);

            string[] stringSize = WxH.Split('x');
            if (stringSize.Length < 2) return false;

            if (int.TryParse(stringSize[0], out int width) && int.TryParse(stringSize[1], out int height))
            {
                resolution.Width = width;
                resolution.Height = height;
                return true;
            }

            return false;
        }
        public static Size ToSize(ImageSize size) => new(size.Width, size.Height);
        public static ImageSize FromFile(string imagePath)
        {
            ImageSize imageSize;
            using (Image image = Image.Load(imagePath))
                imageSize = new(image.Width, image.Height);
            
            return imageSize;
        }

        public static ImageSize operator +(ImageSize left, ImageSize right) => new(left.Width + right.Width, left.Height + right.Height);
        public static ImageSize operator -(ImageSize left, ImageSize right) => new(left.Width - right.Width, left.Height - right.Height);
        public static ImageSize operator *(ImageSize left, ImageSize right) => new(left.Width * right.Width, left.Height * right.Height);
        public static ImageSize operator /(ImageSize left, ImageSize right) => new(left.Width / right.Width, left.Height / right.Height);

        public static ImageSize operator +(ImageSize left, int right) => new(left.Width + right, left.Height + right);
        public static ImageSize operator -(ImageSize left, int right) => new(left.Width - right, left.Height - right);
        public static ImageSize operator *(ImageSize left, int right) => new(left.Width * right, left.Height * right);
        public static ImageSize operator /(ImageSize left, int right) => new(left.Width / right, left.Height / right);

        private static float ClampValue(float value, float min, float max) => (value < min) ? min : (value > max ? max : value);
    }
    public static class ImageHelper
    {
        public static Dictionary<Type, IImageFormat> encoderFormatPairs = new()
        {
            { typeof(PngEncoder), PngFormat.Instance },
            { typeof(JpegEncoder), JpegFormat.Instance},
            { typeof(WebpEncoder), WebpFormat.Instance }
        };

        public static void Resize(string imagePath, int newWidth, int newHeight, IResampler resampler = null)
        {
            using (Image image = Image.Load(imagePath))
            {
                if (resampler == null)
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                else
                    image.Mutate(x => x.Resize(newWidth, newHeight, resampler));

                image.Save(imagePath);
            };
        }
        public static void Resize(string imagePath, ImageSize imageSize, IResampler resampler = null) => Resize(imagePath, imageSize.Width, imageSize.Height, resampler);

        public static bool SetFormat(string imagePath, IImageEncoder encoder)
        {
            using Image image = Image.Load(imagePath);

            if (encoder == null) return false;

            image.Save(imagePath, encoder);
            return true;
        }

        public static IImageEncoder GetEncoder(string targetType) => targetType switch
        {
            "png" => new PngEncoder(),
            "jpg" => new JpegEncoder(),
            "jpeg" => new JpegEncoder(),
            "webp" => new WebpEncoder(),
            _ => null
        };
        public static IImageFormat FileFormat(string imagePath)
        {
            using (FileStream fs = System.IO.File.OpenRead(imagePath))
            {
                return Image.DetectFormat(fs);
            }
        }
        public static IImageFormat EncoderFormat(IImageEncoder encoder)
        {
            if (encoderFormatPairs.TryGetValue(encoder.GetType(), out var format))
            {
                return format;
            }
            else return null;
        }
        public static bool FormatEquals(IImageEncoder encoder, IImageFormat imageFormat) =>
            encoderFormatPairs.TryGetValue(encoder.GetType(), out var format) && format == imageFormat;
        public static bool IsImageIn<T>(string path)
        {
            using (FileStream fs = System.IO.File.OpenRead(path))
            {
                return Image.DetectFormat(fs) is T;
            }
        }
    }
}
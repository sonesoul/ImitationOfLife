using Telegram.Bot.Types;

namespace ImitationOfLife.Interfaces
{
    public interface IFileCommand : ICommand
    {
        public string UsedFileType { get; }
        public Task ExecuteFile(Message message, Document document);
    }
}
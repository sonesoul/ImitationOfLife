using Telegram.Bot.Types;

namespace ImitationOfLife.Interfaces
{
    public interface ITextCommand : ICommand
    {
        public Task ExecuteText(Message message);
    }
}
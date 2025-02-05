namespace ImitationOfLife.Interfaces
{
    public interface ICommand
    {
        string Description { get; }
        string Syntax { get; }
        public string Keyword { get; }
    }
}
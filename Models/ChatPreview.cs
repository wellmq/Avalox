public class ChatPreview
{
    public string Login { get; set; }
    public string LastMessage { get; set; }

    public ChatPreview(string login, string lastMessage)
    {
        Login = login;
        LastMessage = lastMessage;
    }
}

// Sidebar chat preview item
public class ChatPreview
{
    public string Login { get; set; }
    public string LastMessage { get; set; }

    public ChatPreview(string login, string lastMessage)
    {
        Login = login;
        LastMessage = lastMessage;
    }

    public override bool Equals(object? obj) => obj is ChatPreview other && other.Login == Login;
    public override int GetHashCode() => Login.GetHashCode();
}

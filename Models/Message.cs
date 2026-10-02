using System;

// Chat message model
public class Message
{
    public long? Id { get; set; }
    public string Sender { get; set; } = "";
    public string Receiver { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime? Date { get; set; }

    // Ownership flag for UI alignment (sent / received)
    public bool IsMine { get; set; }

    public Message() { }

    public Message(string receiver, string text)
    {
        Receiver = receiver;
        Text = text;
    }
}

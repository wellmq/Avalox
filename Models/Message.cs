using System;

public class Message
{
    public long? Id { get; set; }
    public string Sender { get; set; }
    public string Receiver { get; set; }
    public string Text { get; set; }
    public DateTime? Date { get; set; }

    public Message(string receiver, string text)
    {
        Receiver = receiver;
        Text = text;
    }
}

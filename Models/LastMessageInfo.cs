// Request for messages newer than LastMessageId
public class LastMessageInfo
{
    public long LastMessageId { get; set; }

    public LastMessageInfo(long lastMessageId)
    {
        LastMessageId = lastMessageId;
    }
}

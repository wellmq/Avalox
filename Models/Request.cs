using System.Threading.Tasks;

// Элемент очереди запросов Connection
public class Request
{
    public byte[] Bytes;
    public TaskCompletionSource TaskCS;
    public Response? Response;

    public Request(byte[] bytes)
    {
        Bytes = bytes;
        TaskCS = new TaskCompletionSource();
    }
}

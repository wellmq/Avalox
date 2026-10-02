using System.Net.Sockets;
using System.Net;
using System;
using System.Text.Json;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Threading;

// Подключение к серверу
public class Connection : IDisposable
{
    private const int MaxPacketSize = 5 * 1024 * 1024; // 5 МБ

    private TcpClient? client;
    private NetworkStream? networkStream;

    private readonly Channel<Request> requestChannel;
    private readonly CancellationTokenSource cts = new();

    public bool GetConnectionStatus()
    {
        if (client == null) return false;
        return client.Connected;
    }

    public Connection()
    {
        requestChannel = Channel.CreateUnbounded<Request>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

        Task.Run(ProcessRequestsAsync);
    }

    // Обработка очереди запросов
    private async Task ProcessRequestsAsync()
    {
        try
        {
            await foreach (Request request in requestChannel.Reader.ReadAllAsync(cts.Token))
            {
                await SendRequestAsync(request);
            }
        }
        catch (OperationCanceledException)
        {
            // Отмена при вызове Dispose
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка отправки: {ex.Message}");
        }
    }

    // Подключение к серверу по IP и порту
    public async Task<bool> TryConnect(IPEndPoint ipEndPoint)
    {
        client = new TcpClient();
        try
        {
            await client.ConnectAsync(ipEndPoint);
        }
        catch (Exception)
        {
            return false;
        }

        if (client.Connected)
        {
            networkStream = client.GetStream();
        }

        return client.Connected;
    }

    // Отправка запроса: [тип] + [длина] + [JSON]
    public async Task<Response> MakeRequest(int type, object obj)
    {
        try
        {
            byte[] byteType = [Convert.ToByte(type)];
            string jsonObj = JsonSerializer.Serialize(obj);
            byte[] byteObj = Encoding.UTF8.GetBytes(jsonObj);
            int length = byteObj.Length;
            byte[] byteLength = BitConverter.GetBytes(length);
            byte[] bytes = byteType.Concat(byteLength).Concat(byteObj).ToArray();

            Request request = new Request(bytes);
            if (!requestChannel.Writer.TryWrite(request))
            {
                return new Response { IsSuccessful = false, Message = "Соединение закрыто" };
            }

            await request.TaskCS.Task;
            return request.Response ?? new Response { IsSuccessful = false, Message = "Нет ответа" };
        }
        catch (Exception ex)
        {
            return new Response { IsSuccessful = false, Message = ex.Message };
        }
    }

    // Запись запроса в поток и чтение ответа
    private async Task SendRequestAsync(Request request)
    {
        try
        {
            if (client != null && networkStream != null && client.Connected)
            {
                await networkStream.WriteAsync(request.Bytes, cts.Token);

                // Читаем тип ответа
                byte[] byteType = new byte[1];
                await networkStream.ReadExactlyAsync(byteType, cts.Token);

                // Читаем длину ответа
                byte[] byteLength = new byte[4];
                await networkStream.ReadExactlyAsync(byteLength, cts.Token);
                int length = BitConverter.ToInt32(byteLength, 0);

                if (length < 0 || length > MaxPacketSize)
                {
                    throw new InvalidOperationException($"Недопустимая длина пакета: {length}");
                }

                // Читаем тело ответа
                byte[] byteObj = new byte[length];
                await networkStream.ReadExactlyAsync(byteObj, cts.Token);
                string jsonObj = Encoding.UTF8.GetString(byteObj);
                request.Response = JsonSerializer.Deserialize<Response>(jsonObj);
            }
            else
            {
                request.Response = new Response { IsSuccessful = false, Message = "Нет подключения" };
            }
        }
        catch (Exception ex)
        {
            request.Response = new Response { IsSuccessful = false, Message = ex.Message };
        }
        finally
        {
            request.TaskCS.TrySetResult();
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        requestChannel.Writer.TryComplete();
        try
        {
            networkStream?.Close();
            networkStream?.Dispose();
            client?.Close();
            client?.Dispose();
        }
        catch { }
    }
}

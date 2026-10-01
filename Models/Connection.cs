using System.Net.Sockets;
using System.Net;
using System;
using System.Text.Json;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Connection
{
    private TcpClient? client;
    private NetworkStream? networkStream;
    private Queue<Request> requestQueue;
    public bool GetConnectionStatus()
    {
        if (client == null) return false;
        return client.Connected;
    }

    public Connection()
    {
        requestQueue = new Queue<Request>();
        Task.Run(() =>
        {
            while (true)
            {
                sendRequest();
            }
        });
    }

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
        if (client.Connected) networkStream = client.GetStream();
        return client.Connected;
    }

    public async Task<Response> MakeRequest(int type, object obj)
    {
        byte[] byteType = [Convert.ToByte(type)];
        string jsonObj = JsonSerializer.Serialize(obj);
        byte[] byteObj = Encoding.UTF8.GetBytes(jsonObj);
        int length = byteObj.Length;
        byte[] byteLength = BitConverter.GetBytes(length);
        byte[] bytes = byteType.Concat(byteLength).Concat(byteObj).ToArray();
        Request request = new Request(bytes);
        requestQueue.Enqueue(request);
        await request.TaskCS.Task;
        return request.Response;
    }

    private void sendRequest()
    {
        if (requestQueue.Count < 1) return;
        Request request = requestQueue.Dequeue();
        if (client != null && networkStream != null && client.Connected)
        {
            networkStream.Write(request.Bytes);
            networkStream.ReadByte();
            byte[] byteLength = new byte[4];
            networkStream.ReadExactly(byteLength);
            int length = BitConverter.ToInt32(byteLength);
            byte[] byteObj = new byte[length];
            networkStream.ReadExactly(byteObj);
            string jsonObj = Encoding.UTF8.GetString(byteObj);
            request.Response = JsonSerializer.Deserialize<Response>(jsonObj);
        }
        request.TaskCS.TrySetResult();
    }
}

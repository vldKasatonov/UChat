namespace uchat;

using dto;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;

public class Client
{
    private string _ip;
    private int _port;
    
    public Client(string ip, int port)
    {
        _ip = ip;
        _port = port;
    }
    
    private static Request CreateRequest(CommandType type, object payload)
    {
        return new Request
        {
            Type = type,
            Payload = JsonSerializer.Serialize(payload)
        };
    }
    
    private async Task<Response?> ExecuteRequest(Request request)
    {
        using var client = new TcpClient();

        try
        {
            await client.ConnectAsync(_ip, _port);
            NetworkStream stream = client.GetStream();
            
            string jsonRequest = JsonSerializer.Serialize(request);
            byte[] dataToSend = Encoding.UTF8.GetBytes(jsonRequest);
            await stream.WriteAsync(dataToSend, 0, dataToSend.Length);

            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                string jsonResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                return JsonSerializer.Deserialize<Response>(jsonResponse);
            }
        }
        catch (SocketException e)
        {
            //TODO: make msgbox
        }
        catch (Exception e)
        {
            //TODO: make msgbox
        }
        return null;
    }

    public async Task<bool> Authorise(string username, string password)
    {
        var authReqPaylod = new AuthRequestPayload
        {
            Username = username,
            Password = password,
        };
        var authReq = CreateRequest(CommandType.Authenticate, authReqPaylod);

        var response = await ExecuteRequest(authReq);
        return response?.status == Status.Success;
    }
}
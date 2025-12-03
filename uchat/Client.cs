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
            Payload = JsonSerializer.SerializeToNode(payload)?.AsObject()
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
        try
        {
            var authReqPayload = new LoginRequestPayload
            {
                Username = username,
                Password = password
            };

            var authReq = CreateRequest(CommandType.Login, authReqPayload);

            var response = await ExecuteRequest(authReq);
            return response.Status == Status.Success;
        }
        catch
        {
            throw;
        }
    }

    /*public async Task<bool> Register(string username, string password, string nickname)
    {
        var regPayload = new RegisterRequestPayload
        {
            Username = username,
            Password = password,
            Nickname = nickname
        };

        var regReq = CreateRequest(CommandType.Register, regPayload);

        var response = await ExecuteRequest(regReq);
        return response?.Status == Status.Success;
    } */
    
    public async Task<bool> Register(string username, string password, string nickname)
    {
        await Task.Delay(500);
        if (username == "1")
        {
            return false;
        }
        return true;
    }
    
    /* public async Task<bool> SendMessageAsync(string chatId, Message msg)
    {
        var payload = new { ChatId = chatId, Message = msg };
        var request = CreateRequest(CommandType.SendMessage, payload);
        var response = await ExecuteRequest(request);
        return response?.Status == Status.Success;
    }

    public async Task<bool> DeleteMessageForMeAsync(string chatId, string messageId)
    {
        var payload = new { ChatId = chatId, MessageId = messageId };
        var request = CreateRequest(CommandType.DeleteForMe, payload);
        var response = await ExecuteRequest(request);
        return response?.Status == Status.Success;
    }

    public async Task<bool> DeleteMessageForAllAsync(string chatId, string messageId)
    {
        var payload = new { ChatId = chatId, MessageId = messageId };
        var request = CreateRequest(CommandType.DeleteForAll, payload);
        var response = await ExecuteRequest(request);
        return response?.Status == Status.Success;
    } */
}
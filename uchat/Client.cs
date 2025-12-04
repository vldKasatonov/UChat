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
    private TcpClient _client = null!;
    private NetworkStream _stream = null!;
    private StreamReader _reader = null!;
    private StreamWriter _writer = null!;
    private bool _connected = false;
    private TaskCompletionSource<string>? _pendingResponse;
    
    public Client(string ip, int port)
    {
        _ip = ip;
        _port = port;
    }

    public async Task ConnectToServer()
    {
        while (!_connected)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_ip, _port);

                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                _connected = true;

                _ = Task.Run(ListenServer);
            }
            catch (Exception)
            {
                await Task.Delay(3000); //pause before next try
            }
        }
    }

    private async Task ListenServer()
    {
        try
        {
            while (true)
            {
                string? jsonResponse = await _reader.ReadLineAsync();
                
                if (jsonResponse is null)
                {
                    _connected = false;
                    await ConnectToServer();
                    break;
                }
                
                if (_pendingResponse != null)
                {
                    _pendingResponse.TrySetResult(jsonResponse);
                    _pendingResponse = null;
                    continue;
                }
            }
        }
        catch (Exception)
        {
            _connected = false;
            await ConnectToServer();
        }
    }
    
    private async Task<Response?> ExecuteRequest(Request request)
    {
        if (!_connected)
        {
            await ConnectToServer();
        }
        try
        {
            _pendingResponse = new TaskCompletionSource<string>();
            string jsonRequest = JsonSerializer.Serialize(request);
            await _writer.WriteLineAsync(jsonRequest);

            string jsonResponse = await _pendingResponse.Task;

            return JsonSerializer.Deserialize<Response>(jsonResponse);
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    private static Request CreateRequest(CommandType type, object payload)
    {
        return new Request
        {
            Type = type,
            Payload = JsonSerializer.SerializeToNode(payload)?.AsObject()
        };
    }

    public async Task<Response?> Login(string username, string password)
    {
        var authReqPayload = new LoginRequestPayload
        {
            Username = username,
            Password = password
        };

        var authReq = CreateRequest(CommandType.Login, authReqPayload);
        var response = await ExecuteRequest(authReq);

        if (response != null && response.Payload != null)
        {
            return response;
        }
        
        return null;
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
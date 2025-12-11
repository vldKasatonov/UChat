using dto;
using System.Text;
using System.Text.Json;
using System.Net.Sockets;
using System.Net.Security;

namespace uchat;

public class Client
{
    private string _ip;
    private int _port;
    private TcpClient? _client;
    private int? _clientId;
    private string? _clientUsername;
    private string? _clientNickname;
    private NetworkStream? _networkStream;
    private SslStream? _sslStream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private bool _connected;
    private TaskCompletionSource<string>? _pendingResponse;
    private bool _reconnecting;
    public event Action? Disconnected;
    public event Action? Reconnected;
    public event Action? Shutdown;
    public event Action<Response>? ResponseReceived;
    
    public Client(string ip, int port)
    {
        _ip = ip;
        _port = port;
    }

    public int GetUserId()
    {
        return _clientId ?? 0;
    }
    
    public string GetUsername()
    {
        return _clientUsername ?? "";
    }
    
    public string GetNickname()
    {
        return _clientNickname ?? "";
    }

    public void ClearInfo()
    {
        _clientId = null;
        _clientUsername = null;
        _clientNickname = null;
    }

    public async Task ConnectToServer()
    {
        while (!_connected)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_ip, _port);

                _networkStream = _client.GetStream();
                _sslStream = new SslStream(
                    _networkStream,
                    false,
                    (sender, certificate, chain, sslPolicyErrors) => true
                ); 
                await _sslStream.AuthenticateAsClientAsync(_ip);
                
                _reader = new StreamReader(_sslStream, Encoding.UTF8);
                _writer = new StreamWriter(_sslStream, Encoding.UTF8) { AutoFlush = true };
                _connected = true;

                _ = Task.Run(ListenServer);
            }
            catch (Exception)
            {
                await Task.Delay(5000);
            }
        }
    }

    private async Task ListenServer()
    {
        try
        {
            while (true)
            {
                if (_reader is null)
                {
                    _connected = false;
                    await ConnectToServer();
                    break;
                }
                
                string? jsonResponse = await _reader.ReadLineAsync();
                
                if (jsonResponse is null)
                {
                    await HandleDisconnection();
                    break;
                }
                
                if (_pendingResponse != null)
                {
                    _pendingResponse.TrySetResult(jsonResponse);
                    _pendingResponse = null;
                    continue;
                }

                var response = JsonSerializer.Deserialize<Response>(jsonResponse);

                if (response != null)
                {
                    ResponseReceived?.Invoke(response);
                }
            }
        }
        catch (Exception)
        {
            await HandleDisconnection();
        }
    }

    private async Task HandleDisconnection()
    {
        if (_reconnecting)
        {
            return;
        }

        _reconnecting = true;
        _connected = false;
        Disconnected?.Invoke();
        await ConnectToServer();

        if (_clientId != null && _clientUsername != null)
        {
            var payload = new ReconnectRequestPayload
            {
                UserId = (int)_clientId,
                Username = _clientUsername
            };
    
            var request = CreateRequest(CommandType.Reconnect, payload);
            var response = await ExecuteRequest(request);
    
            if (response != null && response.Status == Status.Success)
            {
                _reconnecting = false;
                Reconnected?.Invoke();
                return;
            }
        }

        Shutdown?.Invoke();
    }
    
    private async Task<Response?> ExecuteRequest(Request request)
    {
        if (!_connected)
        {
            await HandleDisconnection();
        }

        if (_writer is null)
        {
            return null;
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
        var requestPayload = new LoginRequestPayload
        {
            Username = username,
            Password = password
        };

        var request = CreateRequest(CommandType.Login, requestPayload);
        var response = await ExecuteRequest(request);

        if (response != null)
        {
            if (response.Payload != null
                && response.Payload.TryGetPropertyValue("user_id", out var id)
                && response.Payload.TryGetPropertyValue("nickname", out var nickname))
            {
                if (int.TryParse(id?.ToString(), out var parsed))
                {
                    _clientId = parsed;
                    _clientUsername = username;
                    _clientNickname = nickname?.ToString();
                }
                else
                {
                    _clientId = null;
                    _clientUsername = null;
                    _clientNickname = null;
                }
            }
            
            return response;
        }
        
        return null;
    }

    public async Task<Response?> Register(string username, string password, string nickname)
    {
        var requestPayload = new RegisterRequestPayload
        {
            Username = username,
            Password = password,
            Nickname = nickname
        };

        var request = CreateRequest(CommandType.Register, requestPayload);
        var response = await ExecuteRequest(request);

        if (response != null)
        {
            return response;
        }
        
        return null;
    }
    
    public async Task<Response?> CreatePrivateChat(string memberUsername)
    {
        if (_clientId is null || _clientUsername is null)
        {
            return null;
        }

        var requestPayload = new CreateChatRequestPayload
        {
            IsGroup = false,
            Name = null,
            Members = new List<ChatMemberRequest>
            {
                new ChatMemberRequest{Username = _clientUsername, HasPrivileges = true},
                new ChatMemberRequest{Username = memberUsername, HasPrivileges = true}
            }
        };

        var request = CreateRequest(CommandType.CreateChat, requestPayload);
        var response = await ExecuteRequest(request);

        return response;
    }
    
    public async Task<Response?> CreateGroupChat(List<string> membersUsername, string chatName)
    {
        if (_clientId is null || _clientUsername is null)
        {
            return null;
        }
        
        var members = new List<ChatMemberRequest>();

        members.Add(new ChatMemberRequest
        {
            Username = _clientUsername,
            HasPrivileges = true
        });

        foreach (var username in membersUsername)
        {
            members.Add(new ChatMemberRequest
            {
                Username = username,
                HasPrivileges = false
            });
        }

        var requestPayload = new CreateChatRequestPayload
        {
            IsGroup = true,
            Name = chatName,
            Members = members
        };

        var request = CreateRequest(CommandType.CreateChat, requestPayload);
        var response = await ExecuteRequest(request);

        return response;
    }
    
    public async Task<Response?> SendTextMessage(int chatId, Message msg)
    {
        if (_clientId is null)
        {
            return null;
        }

        var requestPayload = new SendTextMessageRequestPayload
        {
            ChatId = chatId,
            SenderId = (int)_clientId,
            Content = msg.Text
        };

        var request = CreateRequest(CommandType.SendMessage, requestPayload);
        var response = await ExecuteRequest(request);

        return response;
    }
    
    public async Task<Response?> SearchUsers(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return null;
        }

        var requestPayload = new SearchUserRequestPayload
        {
            Username = username
        };

        var request = CreateRequest(CommandType.SearchUser, requestPayload);
        var response = await ExecuteRequest(request);

        return response;
    }
    
    public async Task<Response?> GetUserChats()
    {
        if (_clientId is null)
        {
            return null;
        }
        
        var requestPayload = new GetUserChatsRequestPayload { UserId = (int)_clientId };

        var request = CreateRequest(CommandType.GetChats, requestPayload);
        return await ExecuteRequest(request);
    }
    
    public async Task<Response?> GetChatHistory(int chatId, int firstLoadedMessageId, int limit = 150)
    {
        if (_clientId is null)
        {
            return null;
        }
        
        var requestPayload = new ChatHistoryRequestPayload
        {
            ChatId = chatId,
            UserId = (int)_clientId,
            FirstLoadedMessageId = firstLoadedMessageId,
            Limit = limit
        };

        var request = CreateRequest(CommandType.GetHistory, requestPayload);
        var response = await ExecuteRequest(request);

        return response;
    }

    public async Task<Response?> DeleteMessage(int messageId, int chatId)
    {
        if (_clientId is null)
        {
            return null;
        }
        
        var requestPayload = new DeleteMessageRequestPayload
        {
            ChatId = chatId,
            UserId = (int)_clientId,
            MessageId = messageId
        };

        var request = CreateRequest(CommandType.DeleteForAll, requestPayload);
        var response = await ExecuteRequest(request);

        return response;
    }

    public async Task<Response?> EditMessage(int chatId, int messageId, string newContent)
    {
        if (_clientId is null)
        {
            return null;
        }

        var requestPayload = new EditMessageRequestPayload
        {
            ChatId = chatId,
            UserId = (int)_clientId,
            MessageId = messageId,
            NewContent = newContent
        };
        
        var request = CreateRequest(CommandType.EditMessage, requestPayload);
        var response = await ExecuteRequest(request);

        return response;
    }

    public async Task<Response?> UpdateChatPinStatus(int chatId, bool isPinned)
    {
        if (_clientId is null)
        {
            return null;
        }

        var requestPayload = new UpdatePinStatusRequestPayload
        {
            ChatId = chatId,
            UserId = (int)_clientId,
            IsChatPinned = isPinned
        };

        var request = CreateRequest(CommandType.UpdatePinStatus, requestPayload);
        var response = await ExecuteRequest(request);

        return response;
    }
    
    public async Task<Response?> LeaveChat(int chatId)
    {
        if (_clientId is null)
        {
            return null;
        }
    
        var requestPayload = new LeaveChatRequestPayload
        {
            UserId = (int)_clientId,
            ChatId = chatId
        };
    
        var request = CreateRequest(CommandType.LeaveChat, requestPayload);
        var response = await ExecuteRequest(request);
    
        return response;
    }
}
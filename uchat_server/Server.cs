using dto;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using uchat_server.Data;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace uchat_server;

public class Server
{
    private TcpListener _listener;
    private readonly X509Certificate2 _serverCertificate;
    private ConcurrentDictionary<string, int> _userIds = new();
    private ConcurrentDictionary<int, SslStream> _clients = new();
    private readonly DbProvider _dbProvider;
    
    public Server(int port, string dbConnection)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _dbProvider = new DbProvider(dbConnection);
        _serverCertificate = GetServerCertificate();
    }

    public async Task Run()
    {
        _listener.Start();
        Console.WriteLine($"Process ID: {Environment.ProcessId}");
        
        try
        {
            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine("New client connected.");
                _ = HandleClientAsync(client);
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Socket error: {e.Message}");
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        int? userId = null;
        string? username = null;
        NetworkStream networkStream = client.GetStream();
        
        SslStream sslStream = new SslStream(networkStream, false);

        try
        {
            await sslStream.AuthenticateAsServerAsync(_serverCertificate);
            Console.WriteLine("SSL authentication completed.");

            using StreamReader reader = new(sslStream, Encoding.UTF8);
            await using StreamWriter writer = new(sslStream, Encoding.UTF8);
            writer.AutoFlush = true;

            while (client.Connected)
            {
                string? jsonRequest = await reader.ReadLineAsync();
                
                if (string.IsNullOrWhiteSpace(jsonRequest))
                {
                    continue;
                }

                Request? request;

                try
                {
                    request = JsonSerializer.Deserialize<Request>(jsonRequest);
                }
                catch (JsonException)
                {
                    request = null;
                }
                
                if (request is null)
                {
                    string errorResponse = JsonSerializer.Serialize
                    (
                        new Response { Status = Status.Error }
                    );
                    await writer.WriteLineAsync(errorResponse);
                    continue;
                }

                Response response = await ProcessRequest(request);

                string jsonResponse = JsonSerializer.Serialize(response);
                await writer.WriteLineAsync(jsonResponse);

                if (userId is null && response.Status == Status.Success)
                {
                    userId = response.Type switch
                    {
                        CommandType.Login => response.Payload.Deserialize<LoginResponsePayload>()?.UserId,
                        CommandType.Register => response.Payload.Deserialize<RegisterResponsePayload>()?.UserId,
                        CommandType.Reconnect => response.Payload.Deserialize<ReconnectResponsePayload>()?.UserId,
                        _ => null
                    };
                    
                    username = response.Type switch
                    {
                        CommandType.Login => response.Payload.Deserialize<LoginResponsePayload>()?.Username,
                        CommandType.Register => response.Payload.Deserialize<RegisterResponsePayload>()?.Username,
                        CommandType.Reconnect => response.Payload.Deserialize<ReconnectResponsePayload>()?.Username,
                        _ => null
                    };
                    
                    if (userId != null && username != null)
                    {
                        _clients.TryAdd((int)userId, sslStream);
                        _userIds.TryAdd(username, (int)userId);
                        Console.WriteLine($"User '{username}' with ID {userId} is active.");
                    }
                }
                
                Console.WriteLine("Usernames and IDs:");
                foreach (var kvp in _userIds)
                {
                    Console.WriteLine($"Username: {kvp.Key}, UserId: {kvp.Value}");
                }

                Console.WriteLine("UserIds and TcpClients:");
                foreach (var kvp in _clients)
                {
                    Console.WriteLine($"UserId: {kvp.Key}, SSLStream RemoteEndPoint: {kvp.Value}");
                }

                Console.WriteLine($"Received from '{username}' with ID {userId} :\n{jsonRequest}");
                Console.WriteLine($"Response for '{username}' with ID {userId} :\n{JsonSerializer.Serialize(response)}");
            }
        }
        catch (System.Security.Authentication.AuthenticationException ex)
        {
            Console.WriteLine($"TLS Authentication Error: {ex.Message}");
        }
        catch (IOException ex) when (ex.InnerException is SocketException)
        {
            Console.WriteLine($"User '{username}' with ID {userId} disconnected.");
        }
        finally
        {
            if (userId != null && username != null)
            {
                _clients.TryRemove((int)userId, out _);
                _userIds.TryRemove(username, out _);
            }
            
            client.Close();
        }
    }
    
    private async Task<Response> ProcessRequest(Request request)
    {
        try
        {
            switch (request.Type)
            {
                case CommandType.Register:
                    var registerReqPayload = request.Payload.Deserialize<RegisterRequestPayload>();
                    return await HandleRegister(registerReqPayload);
                case CommandType.Login:
                    var loginReqPayload = request.Payload.Deserialize<LoginRequestPayload>();
                    return await HandleLogin(loginReqPayload);
                case CommandType.Reconnect:
                    var reconnectReqPayload = request.Payload.Deserialize<ReconnectRequestPayload>();
                    return await HandleReconnect(reconnectReqPayload);
                case CommandType.CreateChat:
                    var createChatReqPayload = request.Payload.Deserialize<CreateChatRequestPayload>();
                    return await HandleCreateChat(createChatReqPayload);
                case CommandType.SendMessage:
                    var sendReqPayload = request.Payload.Deserialize<SendTextMessageRequestPayload>();
                    return await HandleSendTextMessage(sendReqPayload);
                case CommandType.SearchUser:
                    var searchReqPayload = request.Payload.Deserialize<SearchUserRequestPayload>();
                    return await HandleSearchUsers(searchReqPayload);
                case CommandType.GetChats:
                    var chatsReqPayload = request.Payload.Deserialize<GetUserChatsRequestPayload>();
                    return await HandleGetUserChats(chatsReqPayload);
                case CommandType.GetHistory:
                    var historyReqPayload = request.Payload.Deserialize<ChatHistoryRequestPayload>();
                    return await HandleGetChatHistory(historyReqPayload);
                case CommandType.DeleteForAll:
                    var deleteForAllPayload = request.Payload.Deserialize<DeleteMessageRequestPayload>();
                    return await HandleDeleteForAll(deleteForAllPayload);
            }
        }
        catch (Exception)
        {
            return new Response { Status = Status.Error };
        }
        
        return new Response { Status = Status.Error, Type = request.Type };
    }

    private Task<Response> HandleRegister(RegisterRequestPayload? registerReqPayload)
    {
        if (registerReqPayload is null)
        {
            return Task.FromResult(new Response
            {
                Status = Status.Error,
                Type = CommandType.Register
            });
        }

        try
        {
            string hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(registerReqPayload.Password, workFactor: 12);
            registerReqPayload.Password = hashedPassword;

            User newUser = _dbProvider.RegisterUserAsync(registerReqPayload)
                .GetAwaiter()
                .GetResult();
            
            var responsePayload = new RegisterResponsePayload
            {
                UserId = newUser.Id,
                Nickname = newUser.Nickname,
                Username = newUser.Username
            };

            return Task.FromResult(new Response
            {
                Status = Status.Success,
                Type = CommandType.Register,
                Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
            });
        }
        catch (InvalidOperationException e)
        {
            var errorPayload = new ErrorPayload
            {
                Message = e.Message
            };

            return Task.FromResult(new Response
            {
                Status = Status.Error,
                Type = CommandType.Register,
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            });
        }
    }
    
    private Task<Response> HandleLogin(LoginRequestPayload? loginReqPayload)
    {
        if (loginReqPayload is null)
        {
            return Task.FromResult(new Response
            {
                Status = Status.Error,
                Type = CommandType.Login
            });
        }

        try 
        {
            User loggedUser = _dbProvider.AuthenticateUserAsync(loginReqPayload.Username, loginReqPayload.Password)
                .GetAwaiter()
                .GetResult();

            var responsePayload = new LoginResponsePayload
            {
                UserId = loggedUser.Id,
                Username = loggedUser.Username,
                Nickname = loggedUser.Nickname
            };

            return Task.FromResult(new Response
            {
                Status = Status.Success,
                Type = CommandType.Login,
                Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
            });
        }
        catch (InvalidOperationException e)
        {
            var errorPayload = new ErrorPayload
            {
                Message = e.Message
            };

            return Task.FromResult(new Response
            {
                Status = Status.Error,
                Type = CommandType.Login,
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            });
        }

        // if (loginReqPayload is { Username: "1", Password: "password" })
        // {
        //     var errorPayload = new ErrorPayload
        //     {
        //         Message = "Invalid username or password."
        //     };

        //     return Task.FromResult(new Response
        //     {
        //         Status = Status.Error,
        //         Type = CommandType.Login,
        //         Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
        //     });
        // }

        // var responsePayload = new LoginResponsePayload
        // {
        //     UserId = 1,
        //     Username = loginReqPayload.Username
        // };
        
        // return Task.FromResult(new Response
        // {
        //     Status = Status.Success,
        //     Type = CommandType.Login,
        //     Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
        // });
    }

    private async Task<Response> HandleReconnect(ReconnectRequestPayload? requestPayload)
    {
        if (requestPayload is null)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.Reconnect
            };
        }

        try
        {
            bool userIsValid =
                await _dbProvider.CheckUserExistenceAsync(requestPayload.UserId, requestPayload.Username); 
        
            if (!userIsValid)
            {
                var errorPayload = new ErrorPayload
                {
                    Message = "Invalid User ID or Username provided for reconnect."
                };

                return new Response
                {
                    Status = Status.Error,
                    Type = CommandType.Reconnect,
                    Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
                };
            }
        
            var responsePayload = new ReconnectResponsePayload
            {
                UserId = requestPayload.UserId,
                Username = requestPayload.Username
            };
        
            return new Response
            {
                Status = Status.Success,
                Type = CommandType.Reconnect,
                Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
            };
        }
        catch (Exception)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.Reconnect
            };
        }
    }
    
    private async Task<Response> HandleCreateChat(CreateChatRequestPayload? requestPayload)
    {
        if (requestPayload is null)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.CreateChat
            };
        }

        try
        {
            if (!requestPayload.IsGroup)
            {
                var user1 = requestPayload.Members[0].Username;
                var user2 = requestPayload.Members[1].Username;
                
                var chatId = await _dbProvider.FindExistingPrivateChatAsync(user1, user2);

                if (chatId.HasValue)
                {
                    var errorPayload = new ChatErrorPayload 
                    {
                        ChatId = (int)chatId,
                        Message = $"Private chat with {user2} already exists." 
                    };
                    return new Response
                    {
                        Status = Status.Error,
                        Type = CommandType.CreateChat,
                        Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
                    };
                }
            }
            
            var creationResult = await _dbProvider.CreateChatAsync(requestPayload);
            var newChat = creationResult.Chat;
            var members = creationResult.Members;
            
            var responsePayload = new CreateChatResponsePayload
            {
                ChatId = newChat.Id,
                IsGroup = newChat.IsGroup,
                Name = newChat.Name,
                Members = members
            };

            var response = new Response
            {
                Status = Status.Success,
                Type = CommandType.CreateChat,
                Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
            };

            await BroadcastCreateChat(requestPayload.Members, response);

            return response;
        }
        catch (InvalidOperationException e)
        {
            var errorPayload = new ErrorPayload { Message = e.Message };

            return new Response
            {
                Status = Status.Error,
                Type = CommandType.CreateChat,
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            };
        }
        catch (Exception)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.CreateChat
            };
        }
    }

    private async Task<Response> HandleSendTextMessage(SendTextMessageRequestPayload? requestPayload)
    {
        if (requestPayload is null)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.SendMessage
            };
        }
        
        try
        {
            var savedMessage = await _dbProvider.SaveTextMessageAsync(requestPayload);
        
            string messageContent = requestPayload.Content; 
        
            var responsePayload = new TextMessageResponsePayload
            {
                ChatId = savedMessage.ChatId,
                MessageId = savedMessage.Id,
                SenderId = savedMessage.SenderId,
                SenderNickname = await _dbProvider.GetUserNicknameByIdAsync(savedMessage.SenderId),
                Content = messageContent,
                SentAt = savedMessage.SentAt,
                IsEdited = savedMessage.IsEdited,
                IsDeleted = savedMessage.IsDeleted
            };
        
            var response = new Response
            {
                Status = Status.Success,
                Type = CommandType.SendMessage,
                Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
            };

            var memberIds = await _dbProvider.GetChatMemberIdsAsync(requestPayload.ChatId);
        
            await BroadcastMessage(requestPayload.SenderId, memberIds, response);
        
            return response;
        }
        catch (InvalidOperationException e)
        {
            var errorPayload = new ErrorPayload { Message = e.Message };

            return new Response
            {
                Status = Status.Error,
                Type = CommandType.SendMessage,
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            };
        }
        catch (Exception)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.SendMessage
            };
        }
    }
    
    private async Task<Response> HandleSearchUsers(SearchUserRequestPayload? requestPayload)
    {
        if (requestPayload is null || string.IsNullOrWhiteSpace(requestPayload.Username))
        {
            var errorPayload = new ErrorPayload { Message = "Username query cannot be empty." };

            return new Response
            {
                Status = Status.Error,
                Type = CommandType.SearchUser,
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            };
        }

        try
        {
            var foundUser =
                await _dbProvider.FindUserByUsernameAsync(requestPayload.Username);

            if (foundUser is null)
            {
                return new Response
                {
                    Status = Status.Success,
                    Type = CommandType.SearchUser,
                    Payload = JsonSerializer.SerializeToNode(new SearchUserResponsePayload())?.AsObject()
                };
            }

            var responsePayload = new SearchUserResponsePayload
            {
                UserId = foundUser.Id,
                Username = foundUser.Username,
                Nickname = foundUser.Nickname
            };

            return new Response
            {
                Status = Status.Success,
                Type = CommandType.SearchUser,
                Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
            };
        }
        catch (Exception)
        {
            var errorPayload = new ErrorPayload { Message = "Server error during user search." };

            return new Response
            {
                Status = Status.Error,
                Type = CommandType.SearchUser,
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            };
        }
    }

    private async Task<Response> HandleGetUserChats(GetUserChatsRequestPayload? requestPayload)
    {
        if (requestPayload is null)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.GetChats
            };
        }

        try
        {
            var chatsMetadata = await _dbProvider.GetUserChatsAsync(requestPayload.UserId);

            var responsePayload = new GetUserChatsResponsePayload { Chats = chatsMetadata };

            return new Response
            {
                Status = Status.Success,
                Type = CommandType.GetChats,
                Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
            };
        }
        catch (Exception)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.GetChats                
            };
        }
    }
    
    private async Task<Response> HandleGetChatHistory(ChatHistoryRequestPayload? requestPayload)
    {
        if (requestPayload is null || requestPayload.ChatId <= 0)
        {
            var errorPayload = new ErrorPayload { Message = "Invalid request payload." };
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.GetHistory,
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            };
        }

        try
        {
            int fetchLimit = requestPayload.Limit + 1;

            var history = await _dbProvider.GetHistoryAsync(
                requestPayload.ChatId,
                requestPayload.UserId,
                requestPayload.FirstLoadedMessageId,
                fetchLimit
            );

            bool hasMore = history.Count > requestPayload.Limit;

            if (hasMore)
            {
                history.RemoveAt(history.Count - 1);
            }

            var responsePayload = new ChatHistoryResponsePayload
            {
                Messages = history,
                HasMore = hasMore
            };

            return new Response
            {
                Status = Status.Success,
                Type = CommandType.GetHistory,
                Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
            };
        }
        catch (InvalidOperationException e)
        {
            var errorPayload = new ErrorPayload { Message = e.Message };
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.GetHistory,
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            };
        }
        catch (Exception)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.GetHistory
            };
        }
    }
    
    private async Task BroadcastCreateChat(List<ChatMemberRequest> members, Response response)
    {
        string jsonResponse = JsonSerializer.Serialize(response);
        string creatorUsername = members[0].Username;
        
        foreach (var member in members)
        {
            if (members.Count > 2)
            {
                if (member.Username == creatorUsername)
                {
                    continue;
                }
            }

            if (members.Count == 2)
            {
                if (member.Username == creatorUsername)
                {
                    continue;
                }
            }

            if (_userIds.TryGetValue(member.Username, out var userId))
            {
                if (_clients.TryGetValue(userId, out var sslStream))
                {
                    try
                    {
                        var writer = new StreamWriter(sslStream) { AutoFlush = true };
                        await writer.WriteLineAsync(jsonResponse);
                        Console.WriteLine($"Broadcast (Create chat) for ID {userId}:\n{jsonResponse}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending broadcast to ID {userId}: {ex.Message}");
                    }
                }
            }
        }
    }
    
    private async Task BroadcastMessage(int senderId, List<int> chatMemberIds, Response response)
    {
        string jsonResponse = JsonSerializer.Serialize(response);

        foreach (var memberId in chatMemberIds) 
        {
            if (memberId == senderId)
            {
                continue;
            }

            if (_clients.TryGetValue(memberId, out var sslStream))
            {
                try
                {
                    var writer = new StreamWriter(sslStream) { AutoFlush = true };
                    await writer.WriteLineAsync(jsonResponse);
                    Console.WriteLine($"Broadcast (Message) for ID {memberId}:\n{jsonResponse}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending broadcast to ID {memberId}: {ex.Message}");
                }
            }
        }
    }

    private async Task<Response> HandleDeleteForAll(DeleteMessageRequestPayload? requestPayload)
    {
        if (requestPayload is null)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.DeleteForAll
            };
        }

        try
        {
            var deleteMessageResult = await _dbProvider.DeleteMessageAsync(requestPayload.MessageId, requestPayload.UserId);

            if (deleteMessageResult == null)
            {
                var errorPayload = new ErrorPayload
                {
                    Message = "Message not found or user not authorized to delete this message."
                };

                return new Response
                {
                    Status = Status.Error,
                    Type = CommandType.DeleteForAll,
                    Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
                };
            }
            
            var response = new Response
            {
                Status = Status.Success,
                Type = CommandType.DeleteForAll,
                Payload = JsonSerializer.SerializeToNode(deleteMessageResult)?.AsObject() 
            };

            var memberIds = await _dbProvider.GetChatMemberIdsAsync(deleteMessageResult.ChatId);            
            await BroadcastMessage(requestPayload.UserId, memberIds, response);

            return response;
        }
        catch (InvalidOperationException e)
        {
            var errorPayload = new ErrorPayload
            {
                Message = e.Message
            };

            return new Response
            {
                Status = Status.Error,
                Type = CommandType.DeleteForAll, 
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            };
        }
        catch (Exception)
        {
            var errorPayload = new ErrorPayload
            {
                Message = "An unexpected server error occurred."
            };

            return new Response
            {
                Status = Status.Error,
                Type = CommandType.DeleteForAll,
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            };
        }
    }

    private X509Certificate2 GetServerCertificate()
    {
        string baseDirectory = AppContext.BaseDirectory;
        string certPath = Path.Combine(baseDirectory, "server_certificate.pfx");
        string certPassword = "MySuperSecretPassword"; 

        try
        {
            byte[] certBytes = File.ReadAllBytes(certPath);

            X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12(certBytes, certPassword, X509KeyStorageFlags.DefaultKeySet);
            return certificate;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Certificate upload error: {ex.Message}");
            throw;
        }
    }
}
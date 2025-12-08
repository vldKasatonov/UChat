using dto;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using uchat_server.Data;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using BCrypt.Net;
using ChatMember = dto.ChatMember;

namespace uchat_server;

public class Server
{
    private TcpListener _listener;
    private readonly X509Certificate2 _serverCertificate;
    private ConcurrentDictionary<string, int> _userIds = new();
    private ConcurrentDictionary<int, SslStream> _clients = new();
    private string _dbConnection;
    
    public Server(int port, string dbConnection)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _dbConnection = dbConnection;
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
    
    private UchatDbContext CreateDbContext()
    {
        return new UchatDbContext(_dbConnection); 
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
            using StreamWriter writer = new(sslStream, Encoding.UTF8);
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

            User newUser = RegisterUserAsync(registerReqPayload)
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
        catch (Exception)
        {
            throw;
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
            User loggedUser = AuthenticateUserAsync(loginReqPayload.Username, loginReqPayload.Password)
                .GetAwaiter()
                .GetResult();

            var responsePayload = new LoginResponsePayload
            {
                UserId = loggedUser.Id,
                Username = loggedUser.Username
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
        catch (Exception)
        {
            throw;
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
    
    private async Task<User> RegisterUserAsync(RegisterRequestPayload payload)
    {
        var newUser = new User
        {
            Username = payload.Username,
            Nickname = payload.Nickname,
            HashPassword = payload.Password,
            IsOnline = false,
            Avatar = null
        };

        await using (var dbContext = CreateDbContext())
        {
            try
            {
                dbContext.Users.Add(newUser);
                await dbContext.SaveChangesAsync();
            }
            catch(DbUpdateException e) when ((e.InnerException as PostgresException)?.SqlState == "23505")
            {
                throw new InvalidOperationException("Username already exists");
            }
        }
        
        return newUser;
    }

    private async Task<User> AuthenticateUserAsync(string username, string password)
    {
        await using (var dbContext = CreateDbContext())
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(password, user.HashPassword))
            {
                throw new InvalidOperationException("Invalid username or password.");
            }

            return user;
        }
    }

    private Task<Response> HandleReconnect(ReconnectRequestPayload? requestPayload)
    {
        if (requestPayload is null)
        {
            return Task.FromResult(new Response
            {
                Status = Status.Error,
                Type = CommandType.Reconnect
            });
        }
        
        // TODO: check ID in DB
        
        var responsePayload = new ReconnectResponsePayload
        {
            UserId = requestPayload.UserId,
            Username = requestPayload.Username
        };
        
        return Task.FromResult(new Response
        {
            Status = Status.Success,
            Type = CommandType.Reconnect,
            Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
        });
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
                
                var chatId = await FindExistingPrivateChatAsync(user1, user2);

                if (chatId.HasValue)
                {
                    var errorPayload = new ErrorPayload 
                    { 
                        Message = $"Private chat with {user2} already exists with ID {chatId.Value}." 
                    };
                    return new Response
                    {
                        Status = Status.Error,
                        Type = CommandType.CreateChat,
                        Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
                    };
                }
            }
            
            var newChat = await CreateChatAsync(requestPayload);
            
            var responsePayload = new CreateChatResponsePayload
            {
                ChatId = newChat.Id,
                IsGroup = newChat.IsGroup,
                Name = newChat.Name,
                Members = requestPayload.Members
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
            return new Response { Status = Status.Error, Type = CommandType.CreateChat };
        }
    }

    private async Task BroadcastCreateChat(List<ChatMember> members, Response response)
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
    
    private async Task<int?> FindExistingPrivateChatAsync(string username1, string username2)
    {
        await using var dbContext = CreateDbContext();

        var userIds = await dbContext.Users
            .Where(u => u.Username == username1 || u.Username == username2)
            .Select(u => u.Id)
            .ToListAsync();

        if (userIds.Count < 2)
        {
            throw new InvalidOperationException("One or both users not found in the system.");
        }
    
        int id1 = userIds[0];
        int id2 = userIds.Count > 1 ? userIds[1] : 0;

        var chatId = await dbContext.Chats
            .Where(c => c.IsGroup == false)
            .Where(c => c.Members.Count == 2)
            .Where(c => c.Members.Any(cm => cm.UserId == id1) && c.Members.Any(cm => cm.UserId == id2))
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (chatId != 0)
        {
            return chatId;
        }

        return null;
    }
    
    private async Task<Chat> CreateChatAsync(CreateChatRequestPayload payload)
    {
        await using var dbContext = CreateDbContext();
    
        var newChat = new Chat
        {
            IsGroup = payload.IsGroup,
            Name = payload.IsGroup ? payload.Name : null
        };
    
        dbContext.Chats.Add(newChat);

        var chatMembers = new List<Data.ChatMember>();
    
        var usernames = payload.Members.Select(m => m.Username).ToList();
    
        var dbUsers = await dbContext.Users
            .Where(u => usernames.Contains(u.Username))
            .ToDictionaryAsync(u => u.Username, u => u.Id);

        if (dbUsers.Count != payload.Members.Count)
        {
            throw new InvalidOperationException("One or more chat members were not found.");
        }

        foreach (var member in payload.Members)
        {
            if (dbUsers.TryGetValue(member.Username, out var userId))
            {
                chatMembers.Add(new Data.ChatMember
                {
                    Chat = newChat, 
                    UserId = userId,
                    HasPrivileges = member.HasPrivileges
                });
            }
        }

        dbContext.ChatMembers.AddRange(chatMembers);
    
        await dbContext.SaveChangesAsync();
    
        return newChat;
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
            var savedMessage = await SaveTextMessageAsync(requestPayload);
        
            string messageContent = requestPayload.Content; 
        
            var responsePayload = new TextMessageResponsePayload
            {
                ChatId = savedMessage.ChatId,
                MessageId = savedMessage.Id,
                SenderId = savedMessage.SenderId,
                SenderNickname = await GetUserNicknameByIdAsync(savedMessage.SenderId),
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

            var memberIds = await GetChatMemberIdsAsync(requestPayload.ChatId);
        
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
            return new Response { Status = Status.Error, Type = CommandType.SendMessage };
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
    
    private async Task<Data.Message> SaveTextMessageAsync(SendTextMessageRequestPayload payload)
    {
        var newMessage = new Data.Message
        {
            ChatId = payload.ChatId,
            SenderId = payload.SenderId,
            IsText = true,
            IsEdited = false,
            IsDeleted = false
        };

        var newTextMessage = new TextMessage
        {
            Message = newMessage,
            Content = payload.Content
        };

        await using (var dbContext = CreateDbContext())
        {
            var chatExists = await dbContext.Chats.AnyAsync(c => c.Id == payload.ChatId);
            if (!chatExists)
            {
                throw new InvalidOperationException($"Chat with ID {payload.ChatId} not found.");
            }

            dbContext.Messages.Add(newMessage);
            dbContext.TextMessages.Add(newTextMessage);
        
            await dbContext.SaveChangesAsync();
        }
    
        return newMessage;
    }
    
    private async Task<List<int>> GetChatMemberIdsAsync(int chatId)
    {
        await using (var dbContext = CreateDbContext())
        {
            var memberIds = await dbContext.ChatMembers
                .Where(cm => cm.ChatId == chatId)
                .Select(cm => cm.UserId)
                .ToListAsync();

            if (memberIds.Count == 0)
            {
                throw new InvalidOperationException($"Chat with ID {chatId} not found or has no members.");
            }

            return memberIds;
        }
    }

    private async Task<string> GetUserNicknameByIdAsync(int userId)
    {
        await using var dbContext = CreateDbContext();

        var nickname = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Nickname)
            .FirstOrDefaultAsync();

        return nickname;
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
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
            registerReqPayload.Password = hashedPassword; //??
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

        // TODO: realise login to DB
        
        // get hashedPassword from DB
        // if (!BCrypt.Net.BCrypt.EnhancedVerify(loginReqPayload.Password, hashedPassword))
        // {
        //     var errorPayload = new ErrorPayload
        //     {
        //         Message = "Invalid username or password."
        //     };

        //     return new Response
        //     {
        //         Status = Status.Error,
        //         Type = CommandType.Login,
        //         Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
        //     };
        // }

        if (loginReqPayload is { Username: "1", Password: "password" })
        {
            var errorPayload = new ErrorPayload
            {
                Message = "Invalid username or password."
            };

            return Task.FromResult(new Response
            {
                Status = Status.Error,
                Type = CommandType.Login,
                Payload = JsonSerializer.SerializeToNode(errorPayload)?.AsObject()
            });
        }

        var responsePayload = new LoginResponsePayload
        {
            UserId = int.Parse(loginReqPayload.Username),
            Username = loginReqPayload.Username
        };
        
        return Task.FromResult(new Response
        {
            Status = Status.Success,
            Type = CommandType.Login,
            Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
        });
    }
    
    private async Task<User> RegisterUserAsync(RegisterRequestPayload payload)
    {
        var newUser = new User
        {
            Username = payload.Username,
            Nickname = payload.Nickname,
            HashPassword = payload.Password, //TODO: hash
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

        var responsePayload = new CreateChatResponsePayload
        {
            ChatId = 1, //get from db
            IsGroup = requestPayload.IsGroup,
            Name = requestPayload.Name,
            Members = requestPayload.Members
        };

        var response = new Response
        {
            Status = Status.Success,
            Type = CommandType.CreateChat,
            Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
        };

        await Broadcast(requestPayload.Members, response);
        
        return response;
    }

    private async Task Broadcast(List<ChatMember> members, Response response)
    {
        Console.WriteLine("Broadcast");
        string jsonResponse = JsonSerializer.Serialize(response);
        
        foreach (var member in members)
        {
            if (members.Count > 2 && member.HasPrivileges)
            {
                continue;
            }

            if (members.Count == 2)
            {
                var privatePayload = response.Payload.Deserialize<CreateChatResponsePayload>();
                if (privatePayload != null && privatePayload.Members[0].Username == member.Username)
                {
                    continue;
                }
            }

            if (_userIds.TryGetValue(member.Username, out var userId))
            {
                if (_clients.TryGetValue(userId, out var sslStream))
                {
                    var writer = new StreamWriter(sslStream) { AutoFlush = true };
                    await writer.WriteLineAsync(jsonResponse);
                    Console.WriteLine($"Broadcast for '{member.Username}' with ID {userId}:\n{jsonResponse}");
                }
            }
        }
    }
    
    private Task<Response> HandleSendTextMessage(SendTextMessageRequestPayload? requestPayload) 
    {
        if (requestPayload is null)
        {
            return Task.FromResult(new Response
            {
                Status = Status.Error,
                Type = CommandType.SendMessage
            });
        }
        
        //check if chat_id exists

        var responsePayload = new TextMessageResponsePayload
        {
            ChatId = requestPayload.ChatId,
            SenderId = requestPayload.SenderId,
            Content = requestPayload.Content,
            SentAt = new DateTime(), //get from db
            IsEdited = false,
            IsDeleted = false
        };
        
        return Task.FromResult(new Response
        {
            Status = Status.Success,
            Type = CommandType.SendMessage,
            Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
        });
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
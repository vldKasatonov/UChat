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

namespace uchat_server;

public class Server
{
    private TcpListener _listener;
    private readonly X509Certificate2 _serverCertificate;
    private ConcurrentDictionary<int, TcpClient> _clients = new();
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
                Console.WriteLine("New client connected."); //TODO: delete message
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
                
                //TODO: delete logs
                
                Console.WriteLine($"Received from client: {jsonRequest}");
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
                
                Console.WriteLine($"Response for client: {JsonSerializer.Serialize(response)}");

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
                    
                    if (userId != null)
                    {
                        _clients.TryAdd((int)userId, client);
                        Console.WriteLine($"User '{userId}' is active.");
                    }
                }
            }
        }
        catch (System.Security.Authentication.AuthenticationException ex)
        {
            // Error during SSL/TLS authentication
            Console.WriteLine($"TLS Authentication Error: {ex.Message}");
        }
        catch (IOException ex) when (ex.InnerException is SocketException sockEx)
        {
            // Client disconnected 
            Console.WriteLine($"Client disconnected unexpectedly or reset connection. Message: {sockEx.Message}");
        }
        catch (Exception)
        {
            //Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            if (userId != null)
            {
                _clients.TryRemove((int)userId, out _);
                Console.WriteLine($"User '{userId}' disconnected.");
            }
            
            client.Close();
            Console.WriteLine("Client disconnected.");
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
        
        var responsePayload = new ReconnectResponsePayload()
        {
            UserId = requestPayload.UserId
        };
        
        return Task.FromResult(new Response
        {
            Status = Status.Success,
            Type = CommandType.Reconnect,
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
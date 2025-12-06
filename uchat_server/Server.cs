using dto;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using uchat_server.Data;

namespace uchat_server;

public class Server
{
    private TcpListener _listener;
    private ConcurrentDictionary<string, TcpClient> _clients = new();
    private string _dbConnection;

    public Server(int port, string dbConnection)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _dbConnection = dbConnection;
    }

    public async Task Run()
    {
        _listener.Start();
        //TODO: refactor message
        Console.WriteLine($"Process ID: {Environment.ProcessId}"); 
        Console.WriteLine("Awaiting connections...");
        
        try
        {
            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine("New client connected."); //TODO: refactor message
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
        string? username = string.Empty;
        NetworkStream stream = client.GetStream();
        using StreamReader reader = new(stream, Encoding.UTF8);
        using StreamWriter writer = new(stream, Encoding.UTF8);
        writer.AutoFlush = true;

        try
        {
            while (client.Connected)
            {
                string? jsonRequest = await reader.ReadLineAsync();
                
                if (jsonRequest is null)
                {
                    break;
                }
                
                if (string.IsNullOrWhiteSpace(jsonRequest))
                {
                    continue;
                }
                
                Console.WriteLine($"Received from client: {jsonRequest}");  //TODO: delete message
                Request? request = JsonSerializer.Deserialize<Request>(jsonRequest);
                
                if (request is null)
                {
                    continue; //TODO: return response to user
                }

                Response response = await ProcessRequest(request);
                
                Console.WriteLine($"Response for client: {JsonSerializer.Serialize(response)}");

                if (response.Status == Status.Success)
                {
                    username = response.Type switch
                    {
                        CommandType.Login => response.Payload.Deserialize<LoginResponsePayload>()?.Username,
                        CommandType.Register => response.Payload.Deserialize<RegisterResponsePayload>()?.Username,
                        _ => null
                    };
                    
                    if (!string.IsNullOrEmpty(username))
                    {
                        _clients.TryAdd(username, client);
                        Console.WriteLine($"User '{username}' logged in.");
                    }
                }
                
                string jsonResponse = JsonSerializer.Serialize(response);
                await writer.WriteLineAsync(jsonResponse);
            }
            
        }
        catch (Exception)
        {
            //Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            if (!string.IsNullOrEmpty(username))
            {
                _clients.TryRemove(username, out _);
                Console.WriteLine($"User '{username}' disconnected.");
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
            }
        }
        catch (Exception) //ex)
        {
            //TODO
        }
        
        return new Response
        {
            Status = Status.Error,
            Type = request.Type,
        };
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
    
    private static Task<Response> HandleLogin(LoginRequestPayload? loginReqPayload)
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
            UserId = 1,
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
}
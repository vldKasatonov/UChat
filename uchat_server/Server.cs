using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace uchat_server;

using dto;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;

public class Server
{
    private TcpListener _listener;
    private ConcurrentDictionary<string, TcpClient> _clients = new();

    public Server(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
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

    private async Task HandleClientAsync(TcpClient client)
    {
        string username = string.Empty;
        NetworkStream stream = client.GetStream();
        using StreamReader reader = new(stream, Encoding.UTF8);
        using StreamWriter writer = new(stream, Encoding.UTF8) { AutoFlush = true };

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
                
                Response response = ProcessRequest(request);
                string jsonResponse = JsonSerializer.Serialize(response);
                await writer.WriteLineAsync(jsonResponse);
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine("Client disconnected.");
        }
    }
    
    private static Response ProcessRequest(Request request)
    {
        try
        {
            switch (request.Type)
            {
                case CommandType.Login:
                    var loginReqPayload = request.Payload.Deserialize<LoginRequestPayload>();
                    return HandleLogin(loginReqPayload);
                case CommandType.Register:
                    var registerReqPayload = request.Payload.Deserialize<RegisterRequestPayload>();
                    return HandleRegister(registerReqPayload);
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
    
    private static Response HandleLogin(LoginRequestPayload? loginReqPayload)
    {
        if (loginReqPayload is null)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.Login
            };
        }
        
        // TODO: realise login to DB
        if (loginReqPayload is not { Username: "user", Password: "password" })
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.Login
            };
        }

        var responsePayload = new LoginResponsePayload
        {
            UserId = "1",
            Username = "user"
        };
        
        return new Response
        {
            Status = Status.Success,
            Type = CommandType.Login,
            Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
        };
    }

    private static Response HandleRegister(RegisterRequestPayload? loginReqPayload)
    {
        if (loginReqPayload is null)
        {
            return new Response
            {
                Status = Status.Error,
                Type = CommandType.Register
            };
        }

        // TODO: realise register to DB

        var responsePayload = new RegisterResponsePayload
        {
            UserId = "1",
            Nickname = "qwerty",
            Username = "user"
        };
        
        return new Response
        {
            Status = Status.Success,
            Type = CommandType.Register,
            Payload = JsonSerializer.SerializeToNode(responsePayload)?.AsObject()
        };
    }
}
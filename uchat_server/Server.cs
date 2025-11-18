namespace uchat_server;

using dto;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;

public class Server
{
    public static async Task Run(int port)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, port);

        try
        {
            listener.Start();
            //TODO: refactor message
            Console.WriteLine($"Server started and listening on port {port}.");
            Console.WriteLine($"Process ID: {Environment.ProcessId}"); 
            Console.WriteLine("Awaiting connections...");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("New client connected."); //TODO: refactor message
                _ = Task.Run(() => HandleClientAsync(client));
            }
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Socket error: {e.Message}");
        }
        finally
        {
            listener.Stop();
        }
    }
    
    private static async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        
        try 
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead <= 0)
            {
                return;//TODO: return response to user
            }
            string jsonRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Received from client: {jsonRequest}");  //TODO: delete message
            Request? request = JsonSerializer.Deserialize<Request>(jsonRequest);
            if (request is null)
            {
                return;//TODO: return response to user
            }
            Response response = ProcessRequest(request);
            
            string jsonResponse = JsonSerializer.Serialize(response);
            var responseData = Encoding.UTF8.GetBytes(jsonResponse);
            await stream.WriteAsync(responseData, 0, responseData.Length);
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
                case CommandType.Authenticate:
                    var authReqPayload = JsonSerializer.Deserialize<AuthRequestPayload>(request.Payload);
                    return HandleAuthenticate(authReqPayload);
            }
        }
        catch (Exception) //ex)
        {
            //TODO
        }
        
        return new Response { Status = Status.Error };
    }
    
    private static Response HandleAuthenticate(AuthRequestPayload? authReqPayload)
    {
        if (authReqPayload is null)
        {
            return new Response { Status = Status.Error };
        }
        // TODO: realise authenticate to DB
        if (authReqPayload is { Username: "user", Password: "password" })
        {
            return new Response { Status = Status.Success };
        }
        return new Response { Status = Status.Error };
    }
}
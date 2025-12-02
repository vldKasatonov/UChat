namespace uchat_server;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 1 || !int.TryParse(args[0], out int port))
        {
            Console.WriteLine("Usage: uchat_server <port>"); //TODO: refactor message
            return;
        }

        try
        {
            Server server = new Server(port);
            server.Run().Wait();
        }
        catch (Exception e)
        {
            Console.WriteLine("Server error."); //TODO: refactor message
        }
    }
}
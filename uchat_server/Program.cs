namespace uchat_server;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 1 || !int.TryParse(args[0], out int port))
        {
            Console.WriteLine("Usage: uchat_server <port>");//TODO: refactor message
            return;
        }

        await Server.Run(port);
    }
}
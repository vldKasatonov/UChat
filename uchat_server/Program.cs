using Microsoft.EntityFrameworkCore;
using uchat_server.Config;
using uchat_server.Data;

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

        string dbConnectionString;
        try
        {
            string projectDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uchat_server");
            dbConnectionString = Configer.GetDbConnectionString(projectDirectory); 
            Console.WriteLine("DB configuration loaded successfully.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to configure database connection string. " + e.Message);
            return;
        }
        
        try
        {
            await using var dbContext = new UchatDbContext(dbConnectionString);
            await dbContext.Database.MigrateAsync(); 
            Console.WriteLine("Migration is up to date.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to apply migrations. " + e.Message);
            return;
        }
        
        try
        {
            Server server = new Server(port);
            await server.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Server error: {e.Message}"); //TODO: refactor message
        }
    }
}
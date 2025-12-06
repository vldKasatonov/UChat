using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using uchat_server.Config;

namespace uchat_server.Data;

public class UchatDbContextFactory : IDesignTimeDbContextFactory<UchatDbContext>
{
    public UchatDbContext CreateDbContext(string[] args)
    {
        string dbConnectionString;
        try 
        {   
            dbConnectionString = Configer.GetDbConnectionString();
        }
        catch (InvalidOperationException e)
        {
            throw new InvalidOperationException("Failed to configure database connection string. " + e.Message);
        }

        var optionsBuilder = new DbContextOptionsBuilder<UchatDbContext>();
        optionsBuilder.UseNpgsql(dbConnectionString);
        
        return new UchatDbContext(dbConnectionString); 
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using uchat_server.Config;

namespace uchat_server.Data;

public class UchatDbContextFactory : IDesignTimeDbContextFactory<UchatDbContext>
{
    public UchatDbContext CreateDbContext(string[] args)
    {
        string dbConnectionString = Configer.GetDbConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<UchatDbContext>();
        optionsBuilder.UseNpgsql(dbConnectionString);
        
        return new UchatDbContext(dbConnectionString); 
    }
}
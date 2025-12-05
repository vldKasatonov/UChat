using Microsoft.EntityFrameworkCore;

namespace uchat_server.Data;

public class UchatDbContext : DbContext
{
    private readonly string _dbConnectionString;
    
    public UchatDbContext(string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(_dbConnectionString);
    }
    
    
    public DbSet<User> Users { get; set; }
}
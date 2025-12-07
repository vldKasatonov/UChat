using DotEnv.Core;
using Microsoft.Extensions.Configuration;

namespace uchat_server.Config;

public static class Configer
{
    private const string ConfigFile = "appsettings.json";

    private const string DbConnectionName = "UchatDatabase";
    private static readonly string[] DbEnvPlaceholders =
    [
        "POSTGRES_DB", 
        "POSTGRES_USER", 
        "POSTGRES_PASSWORD"
    ];
    
    public static string GetDbConnectionString(string basePath)
    {
        new EnvLoader().Load();
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(ConfigFile)
            .AddEnvironmentVariables()
            .Build();
        
        string dbConnectionString = configuration.GetConnectionString(DbConnectionName)
                                    ?? throw new InvalidOperationException($"Connection string '{DbConnectionName}' not found in configuration.");
        
        return ReplaceDbEnvPlaceholders(dbConnectionString);
    }

    private static string ReplaceDbEnvPlaceholders(string connection)
    {
        foreach (var placeholder in DbEnvPlaceholders)
        {
            string placeholderToken = $"${{{placeholder}}}";
            if (connection.Contains(placeholderToken))
            {
                string? environmentVariable = Environment.GetEnvironmentVariable(placeholder);
                if (string.IsNullOrEmpty(environmentVariable))
                {
                    throw new InvalidOperationException($"{placeholder} environment variable is not set.");
                }

                connection = connection.Replace(placeholderToken, environmentVariable);
            }
        }
        return connection;
    }
}
using System.Data;
using Dapper;

namespace HelixCarbon.Server.Data;

public static class DatabaseInitializer
{
    public static async Task EnsureSchemaAsync(IDbConnectionFactory factory, IWebHostEnvironment env, ILogger logger)
    {
        using var connection = factory.CreateConnection();
        connection.Open();

        var schemaPath = Path.Combine(env.ContentRootPath, "Data", "Schema.sql");
        await ExecuteScriptAsync(connection, schemaPath, logger);

        if (env.IsDevelopment())
        {
            var seedPath = Path.Combine(env.ContentRootPath, "Data", "Seed.sql");
            await ExecuteScriptAsync(connection, seedPath, logger);
            await EnsureDemoPasswordAsync(connection, logger);
        }
    }

    private static async Task ExecuteScriptAsync(IDbConnection connection, string path, ILogger logger)
    {
        if (!File.Exists(path))
        {
            logger.LogWarning("SQL script not found: {Path}", path);
            return;
        }

        var sql = await File.ReadAllTextAsync(path);
        foreach (var batch in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await connection.ExecuteAsync(batch);
        }
    }

    private static async Task EnsureDemoPasswordAsync(IDbConnection connection, ILogger logger)
    {
        const string email = "admin@demo.local";
        var hash = PasswordHasher.Hash("Admin123!");
        var updated = await connection.ExecuteAsync(
            "UPDATE Users SET PasswordHash = @Hash WHERE Email = @Email",
            new { Hash = hash, Email = email });

        if (updated > 0)
        {
            logger.LogInformation("Demo admin password set for {Email}", email);
        }
    }
}

using System.Data;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace HelixCarbon.Server.Data;

/// <summary>
/// Creates database connections based on appsettings.
/// Extend with MySQL/SQL Server by adding providers and connection string keys.
/// For database-per-tenant, resolve connection string from ITenantContext before calling CreateConnection.
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
    string Provider { get; }
}

public sealed class DbConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    public string Provider { get; } = configuration["Database:Provider"] ?? "Sqlite";

    public IDbConnection CreateConnection() =>
        Provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
            ? new NpgsqlConnection(configuration.GetConnectionString("Postgres"))
            : new SqliteConnection(configuration.GetConnectionString("Sqlite"));
}

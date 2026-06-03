using System.Data;
#if DB_Sqlite
using Microsoft.Data.Sqlite;
#endif
#if DB_Postgres
using Npgsql;
#endif

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

    public IDbConnection CreateConnection()
    {
#if DB_Postgres
        return new NpgsqlConnection(configuration.GetConnectionString("Postgres"));
#elif DB_Sqlite
        return new SqliteConnection(configuration.GetConnectionString("Sqlite"));
#else
        throw new NotSupportedException($"Database provider '{Provider}' is not compiled into this template variant.");
#endif
    }
}

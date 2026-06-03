using System.Data;
using Dapper;
using HelixCarbon.Server.Data;

namespace HelixCarbon.Server.Services;

/// <summary>
/// Base helper enforcing TenantId on queries (shared-database multi-tenancy).
/// For schema-per-tenant, inject a tenant-specific IDbConnection instead.
/// </summary>
public abstract class TenantAwareRepository(IDbConnectionFactory dbFactory, ITenantContext tenantContext)
{
    protected IDbConnectionFactory DbFactory { get; } = dbFactory;
    protected ITenantContext TenantContext { get; } = tenantContext;

    protected Guid RequireTenantId() =>
        TenantContext.TenantId ?? throw new InvalidOperationException("Tenant has not been resolved.");

    protected async Task<T> QuerySingleAsync<T>(string sql, object? param = null)
    {
        using var connection = DbFactory.CreateConnection();
        return await connection.QuerySingleAsync<T>(sql, param);
    }

    protected async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        using var connection = DbFactory.CreateConnection();
        return await connection.QueryAsync<T>(sql, param);
    }

    protected async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using var connection = DbFactory.CreateConnection();
        return await connection.ExecuteAsync(sql, param);
    }
}

using HelixCarbon.Server.Data;
using HelixCarbon.Shared.DTOs;

namespace HelixCarbon.Server.Services;

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetMetricsAsync();
}

public sealed class DashboardService(IDbConnectionFactory dbFactory, ITenantContext tenantContext)
    : IDashboardService
{
    public async Task<DashboardMetricsDto> GetMetricsAsync()
    {
        var tenantId = tenantContext.TenantId ?? throw new InvalidOperationException("Tenant not resolved.");
        using var connection = dbFactory.CreateConnection();

        var productCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Products WHERE TenantId = @TenantId",
            new { TenantId = tenantId.ToString() });

        var rows = await connection.QueryAsync<(string Month, decimal Revenue)>(
            """
            SELECT strftime('%Y-%m', CreatedAt) AS Month, SUM(Price) AS Revenue
            FROM Products WHERE TenantId = @TenantId
            GROUP BY strftime('%Y-%m', CreatedAt)
            ORDER BY Month
            """,
            new { TenantId = tenantId.ToString() });

        var labels = rows.Select(r => r.Month).ToList();
        if (labels.Count == 0)
        {
            labels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"];
        }

        var revenue = rows.Select(r => r.Revenue).ToList();
        while (revenue.Count < labels.Count)
        {
            revenue.Add(0);
        }

        IReadOnlyList<int> counts = Enumerable.Repeat(productCount, labels.Count).ToList();
        return new DashboardMetricsDto(labels, revenue, counts);
    }
}

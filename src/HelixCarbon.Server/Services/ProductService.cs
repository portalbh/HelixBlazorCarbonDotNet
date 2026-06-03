using HelixCarbon.Server.Data;
using HelixCarbon.Shared.DTOs;

namespace HelixCarbon.Server.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> ListAsync();
    Task<ProductDto?> GetAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateProductRequest request);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request);
    Task<bool> DeleteAsync(Guid id);
}

public sealed class ProductService(IDbConnectionFactory dbFactory, ITenantContext tenantContext)
    : TenantAwareRepository(dbFactory, tenantContext), IProductService
{
    public async Task<IReadOnlyList<ProductDto>> ListAsync()
    {
        var tenantId = RequireTenantId();
        var rows = await QueryAsync<ProductRow>(
            """
            SELECT Id, TenantId, Name, Description, Price, CreatedAt
            FROM Products WHERE TenantId = @TenantId
            ORDER BY CreatedAt DESC
            """,
            new { TenantId = tenantId.ToString() });

        return rows.Select(r => Map(RowMapper.ToProduct(r))).ToList();
    }

    public async Task<ProductDto?> GetAsync(Guid id)
    {
        var tenantId = RequireTenantId();
        var row = await QuerySingleOrDefaultAsync<ProductRow>(
            """
            SELECT Id, TenantId, Name, Description, Price, CreatedAt
            FROM Products WHERE Id = @Id AND TenantId = @TenantId
            """,
            new { Id = id.ToString(), TenantId = tenantId.ToString() });

        return row is null ? null : Map(RowMapper.ToProduct(row));
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        var tenantId = RequireTenantId();
        var product = new HelixCarbon.Shared.Models.Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await ExecuteAsync(
            """
            INSERT INTO Products (Id, TenantId, Name, Description, Price, CreatedAt)
            VALUES (@Id, @TenantId, @Name, @Description, @Price, @CreatedAt)
            """,
            new
            {
                Id = product.Id.ToString(),
                TenantId = product.TenantId.ToString(),
                product.Name,
                product.Description,
                product.Price,
                CreatedAt = product.CreatedAt.ToString("O")
            });

        return Map(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        if (await GetAsync(id) is null)
        {
            return null;
        }

        await ExecuteAsync(
            """
            UPDATE Products SET Name = @Name, Description = @Description, Price = @Price
            WHERE Id = @Id AND TenantId = @TenantId
            """,
            new
            {
                Id = id.ToString(),
                TenantId = RequireTenantId().ToString(),
                request.Name,
                request.Description,
                request.Price
            });

        return await GetAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var affected = await ExecuteAsync(
            "DELETE FROM Products WHERE Id = @Id AND TenantId = @TenantId",
            new { Id = id.ToString(), TenantId = RequireTenantId().ToString() });

        return affected > 0;
    }

    private async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null)
    {
        using var connection = DbFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<T>(sql, param);
    }

    private static ProductDto Map(HelixCarbon.Shared.Models.Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.CreatedAt);
}

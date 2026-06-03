using HelixCarbon.Server.Data;
using HelixCarbon.Shared.DTOs;
using HelixCarbon.Shared.Enums;
using HelixCarbon.Shared.Models;

namespace HelixCarbon.Server.Services;

public interface ITenantService
{
    Task<IReadOnlyList<TenantDto>> ListAsync();
    Task<TenantDto?> GetBySlugAsync(string slug);
    Task<TenantDto> OnboardAsync(OnboardingRequest request);
}

public sealed class TenantService(IDbConnectionFactory dbFactory) : ITenantService
{
    public async Task<IReadOnlyList<TenantDto>> ListAsync()
    {
        using var connection = dbFactory.CreateConnection();
        var rows = await connection.QueryAsync<TenantRow>(
            "SELECT Id, Slug, Name, Plan, CreatedAt FROM Tenants ORDER BY CreatedAt DESC");
        return rows.Select(r => Map(RowMapper.ToTenant(r))).ToList();
    }

    public async Task<TenantDto?> GetBySlugAsync(string slug)
    {
        using var connection = dbFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<TenantRow>(
            "SELECT Id, Slug, Name, Plan, CreatedAt FROM Tenants WHERE Slug = @Slug",
            new { Slug = slug.ToLowerInvariant() });

        return row is null ? null : Map(RowMapper.ToTenant(row));
    }

    public async Task<TenantDto> OnboardAsync(OnboardingRequest request)
    {
        using var connection = dbFactory.CreateConnection();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = request.TenantSlug.ToLowerInvariant(),
            Name = request.TenantName,
            Plan = request.Plan,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await connection.ExecuteAsync(
            """
            INSERT INTO Tenants (Id, Slug, Name, Plan, CreatedAt)
            VALUES (@Id, @Slug, @Name, @Plan, @CreatedAt)
            """,
            new
            {
                Id = tenant.Id.ToString(),
                tenant.Slug,
                tenant.Name,
                Plan = (int)tenant.Plan,
                CreatedAt = tenant.CreatedAt.ToString("O")
            });

        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = request.AdminEmail.ToLowerInvariant(),
            PasswordHash = PasswordHasher.Hash(request.AdminPassword),
            Role = TenantRole.Admin,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await connection.ExecuteAsync(
            """
            INSERT INTO Users (Id, TenantId, Email, PasswordHash, Role, CreatedAt)
            VALUES (@Id, @TenantId, @Email, @PasswordHash, @Role, @CreatedAt)
            """,
            new
            {
                Id = user.Id.ToString(),
                TenantId = user.TenantId.ToString(),
                user.Email,
                user.PasswordHash,
                Role = (int)user.Role,
                CreatedAt = user.CreatedAt.ToString("O")
            });

        return Map(tenant);
    }

    private static TenantDto Map(Tenant t) =>
        new(t.Id, t.Slug, t.Name, t.Plan, t.CreatedAt);
}

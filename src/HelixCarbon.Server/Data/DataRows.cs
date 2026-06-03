namespace HelixCarbon.Server.Data;

internal sealed class TenantRow
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Plan { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

internal sealed class ProductRow
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

internal sealed class UserRow
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Role { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

internal static class RowMapper
{
    public static HelixCarbon.Shared.Models.Tenant ToTenant(TenantRow row) => new()
    {
        Id = Guid.Parse(row.Id),
        Slug = row.Slug,
        Name = row.Name,
        Plan = (HelixCarbon.Shared.Enums.SubscriptionPlan)row.Plan,
        CreatedAt = DateTimeOffset.Parse(row.CreatedAt)
    };

    public static HelixCarbon.Shared.Models.Product ToProduct(ProductRow row) => new()
    {
        Id = Guid.Parse(row.Id),
        TenantId = Guid.Parse(row.TenantId),
        Name = row.Name,
        Description = row.Description,
        Price = row.Price,
        CreatedAt = DateTimeOffset.Parse(row.CreatedAt)
    };

    public static HelixCarbon.Shared.Models.UserAccount ToUser(UserRow row) => new()
    {
        Id = Guid.Parse(row.Id),
        TenantId = Guid.Parse(row.TenantId),
        Email = row.Email,
        PasswordHash = row.PasswordHash,
        Role = (HelixCarbon.Shared.Enums.TenantRole)row.Role,
        CreatedAt = DateTimeOffset.Parse(row.CreatedAt)
    };
}

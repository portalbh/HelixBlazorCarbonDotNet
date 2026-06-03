using HelixCarbon.Shared.Enums;

namespace HelixCarbon.Shared.Models;

public sealed class UserAccount
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public TenantRole Role { get; set; } = TenantRole.User;
    public DateTimeOffset CreatedAt { get; set; }
}

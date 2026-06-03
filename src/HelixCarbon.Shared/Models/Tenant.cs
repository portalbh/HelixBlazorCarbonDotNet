using HelixCarbon.Shared.Enums;

namespace HelixCarbon.Shared.Models;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public DateTimeOffset CreatedAt { get; set; }
}

using HelixCarbon.Shared.Enums;

namespace HelixCarbon.Shared.DTOs;

public sealed record TenantDto(
    Guid Id,
    string Slug,
    string Name,
    SubscriptionPlan Plan,
    DateTimeOffset CreatedAt);

using HelixCarbon.Shared.Enums;

namespace HelixCarbon.Shared.DTOs;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(string Email, string Password, TenantRole Role = TenantRole.User);

public sealed record OnboardingRequest(
    string TenantSlug,
    string TenantName,
    string AdminEmail,
    string AdminPassword,
    SubscriptionPlan Plan = SubscriptionPlan.Free);

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    TenantRole Role,
    Guid TenantId,
    string TenantSlug,
    string TenantName,
    SubscriptionPlan Plan);

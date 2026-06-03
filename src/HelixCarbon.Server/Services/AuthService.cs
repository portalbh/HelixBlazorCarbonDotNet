using System.Security.Claims;
using HelixCarbon.Server.Data;
using HelixCarbon.Shared.DTOs;
using HelixCarbon.Shared.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HelixCarbon.Server.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error)> LoginAsync(HttpContext httpContext, LoginRequest request);
    Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request);
    Task<UserProfileDto?> GetProfileAsync();
    Task LogoutAsync(HttpContext httpContext);
}

public sealed class AuthService(
    IDbConnectionFactory dbFactory,
    ITenantContext tenantContext) : IAuthService
{
    public async Task<(bool Success, string? Error)> LoginAsync(HttpContext httpContext, LoginRequest request)
    {
        if (!tenantContext.IsResolved)
        {
            return (false, "Tenant is required.");
        }

        using var connection = dbFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            """
            SELECT Id, TenantId, Email, PasswordHash, Role, CreatedAt
            FROM Users WHERE TenantId = @TenantId AND Email = @Email
            """,
            new
            {
                TenantId = tenantContext.TenantId!.Value.ToString(),
                Email = request.Email.ToLowerInvariant()
            });

        if (row is null || !PasswordHasher.Verify(request.Password, row.PasswordHash))
        {
            return (false, "Invalid email or password.");
        }

        var user = RowMapper.ToUser(row);
        await SignInAsync(httpContext, user, tenantContext.Tenant!);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        if (!tenantContext.IsResolved)
        {
            return (false, "Tenant is required.");
        }

        using var connection = dbFactory.CreateConnection();
        var exists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Users WHERE TenantId = @TenantId AND Email = @Email",
            new
            {
                TenantId = tenantContext.TenantId!.Value.ToString(),
                Email = request.Email.ToLowerInvariant()
            });

        if (exists > 0)
        {
            return (false, "Email is already registered for this tenant.");
        }

        var user = new HelixCarbon.Shared.Models.UserAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId!.Value,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.Role,
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

        return (true, null);
    }

    public Task<UserProfileDto?> GetProfileAsync()
    {
        if (!tenantContext.IsResolved || tenantContext.Tenant is null)
        {
            return Task.FromResult<UserProfileDto?>(null);
        }

        // Profile is built from claims in endpoints when authenticated.
        return Task.FromResult<UserProfileDto?>(null);
    }

    public async Task LogoutAsync(HttpContext httpContext) =>
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    public static async Task SignInAsync(
        HttpContext httpContext,
        HelixCarbon.Shared.Models.UserAccount user,
        HelixCarbon.Shared.Models.Tenant tenant)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("tenant_id", tenant.Id.ToString()),
            new("tenant_slug", tenant.Slug)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }
}

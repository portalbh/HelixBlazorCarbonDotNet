using HelixCarbon.Server.Data;
using HelixCarbon.Server.Services;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HelixCarbon.Server.Middleware;

/// <summary>
/// Resolves tenant from subdomain ({slug}.localhost) or X-Tenant header.
/// To use database-per-tenant, look up a connection string by slug here instead of shared ITenantContext.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string TenantHeaderName = "X-Tenant";

    private static readonly PathString[] BypassPaths =
    [
        "/api/health",
        "/api/onboarding",
        "/_framework",
        "/_content",
        "/lib",
        "/favicon"
    ];

    public async Task InvokeAsync(
        HttpContext context,
        IDbConnectionFactory dbFactory,
        ITenantContext tenantContext)
    {
        if (ShouldBypass(context.Request.Path))
        {
            await next(context);
            return;
        }

        var slug = ResolveSlug(context);
        if (string.IsNullOrWhiteSpace(slug))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            StampNoStore(context.Response.Headers);
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Tenant not specified. Use subdomain ({slug}.localhost) or X-Tenant header."
            });
            return;
        }

        using var connection = dbFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<TenantRow>(
            "SELECT Id, Slug, Name, Plan, CreatedAt FROM Tenants WHERE Slug = @Slug",
            new { Slug = slug.ToLowerInvariant() });

        if (row is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            StampNoStore(context.Response.Headers);
            await context.Response.WriteAsJsonAsync(new { error = $"Tenant '{slug}' was not found." });
            return;
        }

        var tenant = RowMapper.ToTenant(row);
        tenantContext.SetTenant(tenant);
        context.Items["TenantSlug"] = tenant.Slug;
        context.Response.OnStarting(() =>
        {
            StampNoStore(context.Response.Headers);
            AppendVary(context.Response.Headers, TenantHeaderName);
            AppendVary(context.Response.Headers, HeaderNames.Cookie);
            return Task.CompletedTask;
        });
        await next(context);
    }

    private static bool ShouldBypass(PathString path)
    {
        foreach (var bypass in BypassPaths)
        {
            if (path.StartsWithSegments(bypass, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return path.HasValue && path.Value!.Contains('.', StringComparison.Ordinal) &&
               (path.Value.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                path.Value.EndsWith(".js", StringComparison.OrdinalIgnoreCase));
    }

    private string? ResolveSlug(HttpContext context)
    {
        if (configuration.GetValue("App:AllowTenantHeader", false) &&
            context.Request.Headers.TryGetValue(TenantHeaderName, out var header) &&
            !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString().Trim().ToLowerInvariant();
        }

        var host = context.Request.Host.Host;
        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[0] is not "www" and not "localhost")
        {
            return parts[0].ToLowerInvariant();
        }

        if (parts.Length >= 3 && parts[0] is not "www")
        {
            return parts[0].ToLowerInvariant();
        }

        return null;
    }

    private static void StampNoStore(IHeaderDictionary headers)
    {
        headers[HeaderNames.CacheControl] = "no-store, no-cache";
        headers[HeaderNames.Pragma] = "no-cache";
    }

    private static void AppendVary(IHeaderDictionary headers, string value)
    {
        var existing = headers[HeaderNames.Vary];
        if (existing.Any(header => header is not null && header
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Any(part => string.Equals(part, value, StringComparison.OrdinalIgnoreCase))))
        {
            return;
        }

        headers[HeaderNames.Vary] = StringValues.Concat(existing, value);
    }
}

namespace HelixCarbon.Client.Services;

/// <summary>
/// Sends X-Tenant for local dev when subdomain routing is unavailable.
/// Remove or replace with subdomain-only resolution in production.
/// </summary>
public sealed class TenantHeaderHandler : DelegatingHandler
{
    public const string DefaultTenantSlug = "demo";
    public string TenantSlug { get; set; } = DefaultTenantSlug;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains("X-Tenant"))
        {
            request.Headers.TryAddWithoutValidation("X-Tenant", TenantSlug);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

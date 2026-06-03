namespace HelixCarbon.Server.Extensions;

/// <summary>
/// Forwards incoming request context to internal HttpClient calls during server prerender.
/// </summary>
public sealed class ServerRequestForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        if (context.Request.Headers.TryGetValue("Cookie", out var cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie.ToString());
        }

        if (!request.Headers.Contains("X-Tenant"))
        {
            var slug = ResolveTenantSlug(context.Request.Host.Host);
            if (!string.IsNullOrEmpty(slug))
            {
                request.Headers.TryAddWithoutValidation("X-Tenant", slug);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static string? ResolveTenantSlug(string host)
    {
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
}

using HelixCarbon.Client.Services;

namespace HelixCarbon.Server.Extensions;

public static class ClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers API client services for server-side prerender of Interactive WebAssembly pages.
    /// </summary>
    public static IServiceCollection AddHelixCarbonClientForServerPrerender(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuthSessionSignal>();
        services.AddScoped<TenantHeaderHandler>();
        services.AddScoped<UnauthorizedResponseHandler>();
        services.AddScoped<ServerRequestForwardingHandler>();
        services.AddScoped<HelixApiClient>();
        services.AddScoped<AuthStateService>();
        services.AddScoped(sp =>
        {
            var forwarding = sp.GetRequiredService<ServerRequestForwardingHandler>();
            forwarding.InnerHandler = new HttpClientHandler { AllowAutoRedirect = false };

            var unauthorizedHandler = sp.GetRequiredService<UnauthorizedResponseHandler>();
            unauthorizedHandler.InnerHandler = forwarding;

            var tenantHandler = sp.GetRequiredService<TenantHeaderHandler>();
            tenantHandler.InnerHandler = unauthorizedHandler;

            var context = sp.GetRequiredService<IHttpContextAccessor>().HttpContext
                ?? throw new InvalidOperationException("HttpContext is required for prerender.");

            var request = context.Request;
            var port = request.Host.Port ?? context.Connection.LocalPort;
            var baseUri = new Uri($"{request.Scheme}://127.0.0.1:{port}/");
            return new HttpClient(tenantHandler) { BaseAddress = baseUri };
        });

        return services;
    }
}

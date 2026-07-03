using HelixCarbon.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HelixCarbon.Client;

public static class HelixCarbonClientServiceCollectionExtensions
{
    public static IServiceCollection AddHelixCarbonWasmClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddScoped<AuthSessionSignal>();
        services.AddScoped<TenantHeaderHandler>();
        services.AddScoped<UnauthorizedResponseHandler>();
        services.AddScoped<HelixApiClient>();
        services.AddScoped<AuthStateService>();
        services.AddScoped(sp =>
        {
            var unauthorizedHandler = sp.GetRequiredService<UnauthorizedResponseHandler>();
            unauthorizedHandler.InnerHandler = new HttpClientHandler();

            var handler = sp.GetRequiredService<TenantHeaderHandler>();
            handler.InnerHandler = unauthorizedHandler;
            return new HttpClient(handler) { BaseAddress = baseAddress };
        });

        return services;
    }
}

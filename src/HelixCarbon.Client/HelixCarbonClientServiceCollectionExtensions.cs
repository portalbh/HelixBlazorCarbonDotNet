using HelixCarbon.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HelixCarbon.Client;

public static class HelixCarbonClientServiceCollectionExtensions
{
    public static IServiceCollection AddHelixCarbonWasmClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddScoped<TenantHeaderHandler>();
        services.AddScoped<HelixApiClient>();
        services.AddScoped<AuthStateService>();
        services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<TenantHeaderHandler>();
            handler.InnerHandler = new HttpClientHandler();
            return new HttpClient(handler) { BaseAddress = baseAddress };
        });

        return services;
    }
}

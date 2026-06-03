using HelixCarbon.Server.Data;
using HelixCarbon.Server.Services;
#if (AuthAzure)
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
#endif
#if (AuthBFF || AuthAdvanced)
using Microsoft.AspNetCore.Authentication.Cookies;
#endif

namespace HelixCarbon.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelixCarbonData(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IDashboardService, DashboardService>();
#if (AuthBFF || AuthAdvanced)
        services.AddScoped<IAuthService, AuthService>();
#endif
        return services;
    }

    public static IServiceCollection AddHelixCarbonAuth(this IServiceCollection services, IConfiguration configuration)
    {
#if (AuthNone)
        services.AddAuthorization();
#elif (AuthAzure)
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));
        services.AddAuthorization();
#elif (AuthBFF || AuthAdvanced)
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "HelixCarbon.Auth";
                options.LoginPath = "/login";
                options.LogoutPath = "/api/auth/logout";
            });
        services.AddAuthorization();
#else
        services.AddAuthorization();
#endif
        return services;
    }
}

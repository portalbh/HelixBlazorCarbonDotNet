using ApexCharts;
using CarbonBlazor;
using HelixCarbon.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#if (AuthAzure)
using Microsoft.Authentication.WebAssembly.Msal;
using Microsoft.Authentication.WebAssembly.Msal.Models;
#endif

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCarbonBlazor();
builder.Services.AddApexCharts();

builder.Services.AddScoped<TenantHeaderHandler>();
builder.Services.AddScoped<HelixApiClient>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<TenantHeaderHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});

#if (AuthAzure)
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    var scope = builder.Configuration["AzureAd:DefaultScope"];
    if (!string.IsNullOrWhiteSpace(scope))
    {
        options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
    }
});
#endif

#if (AuthBFF || AuthAdvanced)
builder.Services.AddAuthorizationCore();
#endif

await builder.Build().RunAsync();

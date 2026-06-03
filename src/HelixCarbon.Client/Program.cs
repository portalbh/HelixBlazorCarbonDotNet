using ApexCharts;
using CarbonBlazor;
using HelixCarbon.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#if AuthAzure
using Microsoft.Authentication.WebAssembly.Msal;
#endif

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCarbonBlazor();
builder.Services.AddApexCharts();
builder.Services.AddHelixCarbonWasmClient(new Uri(builder.HostEnvironment.BaseAddress));

#if AuthAzure
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

#if AuthBFF || AuthAdvanced
builder.Services.AddAuthorizationCore();
#endif

await builder.Build().RunAsync();

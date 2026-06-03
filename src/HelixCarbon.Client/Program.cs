using CarbonBlazor;
using HelixCarbon.Client;
using HelixCarbon.Client.Charts;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#if AuthAzure
using Microsoft.Authentication.WebAssembly.Msal;
#endif

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddCarbonBlazor();
builder.Services.AddHelixCharts();
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

#if AuthAzure
builder.Services.AddAuthorizationCore();
#endif

#if AuthBFF || AuthAdvanced
builder.Services.AddAuthorizationCore();
#endif

await builder.Build().RunAsync();

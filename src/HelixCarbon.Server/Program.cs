using HelixCarbon.Client;
using HelixCarbon.Server.Components;
using HelixCarbon.Server.Data;
using HelixCarbon.Server.Endpoints;
using HelixCarbon.Server.Extensions;
using HelixCarbon.Server.Middleware;
using CarbonBlazor;
#if AuthAzure
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
#endif
#if AuthBFF || AuthAdvanced
using Microsoft.AspNetCore.Authentication.Cookies;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHelixCarbonData();
builder.Services.AddHelixCarbonAuth(builder.Configuration);
builder.Services.AddCarbonBlazor();
builder.Services.AddHelixCarbonClientForServerPrerender();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient();

var app = builder.Build();

await DatabaseInitializer.EnsureSchemaAsync(
    app.Services.GetRequiredService<IDbConnectionFactory>(),
    app.Environment,
    app.Configuration,
    app.Logger);

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();
app.UseAntiforgery();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Response.Headers.CacheControl = "no-store, no-cache";
            context.Response.Headers.Pragma = "no-cache";
        }

        return Task.CompletedTask;
    });

    await next();
});

#if AuthAzure
app.UseAuthentication();
app.UseAuthorization();
#elif AuthBFF || AuthAdvanced
app.UseAuthentication();
app.UseAuthorization();
#endif

app.UseMiddleware<TenantResolutionMiddleware>();

app.MapHelixCarbonApi();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HelixCarbon.Client._Imports).Assembly);

app.Run();

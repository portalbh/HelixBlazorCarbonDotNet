using System.Security.Claims;
using HelixCarbon.Server.Services;
using HelixCarbon.Shared.DTOs;
using HelixCarbon.Shared.Enums;
using Microsoft.AspNetCore.Authorization;

namespace HelixCarbon.Server.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapHelixCarbonApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/health", () => Results.Ok(new { status = "healthy", template = "HelixCarbon" }))
            .AllowAnonymous();

        api.MapPost("/onboarding", async (OnboardingRequest request, ITenantService tenants) =>
        {
            try
            {
                var tenant = await tenants.OnboardAsync(request);
                return Results.Created($"/api/tenants/{tenant.Slug}", tenant);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).AllowAnonymous();

#if (AuthNone)
        api.MapGet("/tenants", async (ITenantService tenants) =>
            Results.Ok(await tenants.ListAsync())).AllowAnonymous();

        api.MapGet("/dashboard/metrics", async (IDashboardService dashboard) =>
            Results.Ok(await dashboard.GetMetricsAsync())).AllowAnonymous();
#else
        api.MapGet("/tenants", async (ITenantService tenants) =>
            Results.Ok(await tenants.ListAsync()))
            .RequireAuthorization(policy => policy.RequireRole(nameof(TenantRole.Admin)));

        api.MapGet("/dashboard/metrics", async (IDashboardService dashboard) =>
            Results.Ok(await dashboard.GetMetricsAsync()))
            .RequireAuthorization();
#endif

        MapProductEndpoints(api);
        MapAuthEndpoints(api);

        return app;
    }

    private static void MapProductEndpoints(RouteGroupBuilder api)
    {
#if (AuthNone)
        var products = api.MapGroup("/products");
#else
        var products = api.MapGroup("/products").RequireAuthorization();
#endif

        products.MapGet("/", async (IProductService service) =>
            Results.Ok(await service.ListAsync()));

        products.MapGet("/{id:guid}", async (Guid id, IProductService service) =>
            await service.GetAsync(id) is { } product ? Results.Ok(product) : Results.NotFound());

        products.MapPost("/", async (CreateProductRequest request, IProductService service) =>
        {
            var created = await service.CreateAsync(request);
            return Results.Created($"/api/products/{created.Id}", created);
        });

        products.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest request, IProductService service) =>
            await service.UpdateAsync(id, request) is { } updated ? Results.Ok(updated) : Results.NotFound());

        products.MapDelete("/{id:guid}", async (Guid id, IProductService service) =>
            await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());
    }

    private static void MapAuthEndpoints(RouteGroupBuilder api)
    {
#if (AuthNone)
        // No cookie/JWT auth endpoints in None mode.
#elif (AuthAzure)
        // Azure mode uses MSAL on the client and JWT bearer on the API (configured in Program.cs).
#else
        var auth = api.MapGroup("/auth");

        auth.MapPost("/login", async (LoginRequest request, IAuthService authService, HttpContext ctx) =>
        {
            var (success, error) = await authService.LoginAsync(ctx, request);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        }).AllowAnonymous();

        auth.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var (success, error) = await authService.RegisterAsync(request);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        }).RequireAuthorization(policy => policy.RequireRole(nameof(TenantRole.Admin)));

        auth.MapPost("/logout", async (IAuthService authService, HttpContext ctx) =>
        {
            await authService.LogoutAsync(ctx);
            return Results.Ok();
        }).RequireAuthorization();

        auth.MapGet("/profile", (ClaimsPrincipal user, ITenantContext tenant) =>
        {
            if (!tenant.IsResolved || tenant.Tenant is null)
            {
                return Results.BadRequest(new { error = "Tenant not resolved." });
            }

            var email = user.FindFirstValue(ClaimTypes.Email) ?? "unknown";
            var role = Enum.TryParse<TenantRole>(user.FindFirstValue(ClaimTypes.Role), out var r)
                ? r
                : TenantRole.User;

            return Results.Ok(new UserProfileDto(
                Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString()),
                email,
                role,
                tenant.Tenant.Id,
                tenant.Tenant.Slug,
                tenant.Tenant.Name,
                tenant.Tenant.Plan));
        }).RequireAuthorization();
#endif
    }
}

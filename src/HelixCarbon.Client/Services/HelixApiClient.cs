using System.Net.Http.Json;
using HelixCarbon.Shared.DTOs;

namespace HelixCarbon.Client.Services;

public sealed class HelixApiClient(HttpClient http)
{
    public async Task<DashboardMetricsDto?> GetDashboardMetricsAsync() =>
        await http.GetFromJsonAsync<DashboardMetricsDto>("api/dashboard/metrics");

    public async Task<IReadOnlyList<ProductDto>?> GetProductsAsync() =>
        await http.GetFromJsonAsync<IReadOnlyList<ProductDto>>("api/products");

    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request)
    {
        var response = await http.PostAsJsonAsync("api/products", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ProductDto>()
            : null;
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        var response = await http.DeleteAsync($"api/products/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<TenantDto>?> GetTenantsAsync() =>
        await http.GetFromJsonAsync<IReadOnlyList<TenantDto>>("api/tenants");

    public async Task<TenantDto?> OnboardAsync(OnboardingRequest request)
    {
        var response = await http.PostAsJsonAsync("api/onboarding", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TenantDto>()
            : null;
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        var response = await http.PostAsJsonAsync("api/auth/login", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var response = await http.PostAsJsonAsync("api/auth/register", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<UserProfileDto?> GetProfileAsync() =>
        await http.GetFromJsonAsync<UserProfileDto>("api/auth/profile");

    public async Task LogoutAsync() => await http.PostAsync("api/auth/logout", null);
}

using System.Net.Http.Json;
using HelixCarbon.Shared.DTOs;

namespace HelixCarbon.Client.Services;

public sealed class HelixApiClient(HttpClient http)
{
    public async Task<DashboardMetricsDto?> GetDashboardMetricsAsync() =>
        await GetJsonOrNullAsync<DashboardMetricsDto>("api/dashboard/metrics");

    public async Task<IReadOnlyList<ProductDto>?> GetProductsAsync() =>
        await GetJsonOrNullAsync<IReadOnlyList<ProductDto>>("api/products");

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
        await GetJsonOrNullAsync<IReadOnlyList<TenantDto>>("api/tenants");

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
        await GetJsonOrNullAsync<UserProfileDto>("api/auth/profile");

    public async Task LogoutAsync() => await http.PostAsync("api/auth/logout", null);

    private async Task<T?> GetJsonOrNullAsync<T>(string requestUri)
    {
        try
        {
            var response = await http.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (HttpRequestException)
        {
            return default;
        }
    }
}

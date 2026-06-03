using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace CarbonBlazor;

public static class CarbonBlazorServiceCollectionExtensions
{
    public static IServiceCollection AddCarbonBlazor(this IServiceCollection services)
    {
        services.AddScoped<CarbonBlazorJsModule>();
        return services;
    }
}

public sealed class CarbonBlazorJsModule(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() =>
        jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/CarbonBlazor/carbon-blazor.js").AsTask());

    public Task<IJSObjectReference> GetModuleAsync() => _moduleTask.Value;

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }
    }
}

using ApexCharts;
using Microsoft.Extensions.DependencyInjection;

namespace HelixCarbon.Client.Charts;

public static class ChartsServiceCollectionExtensions
{
    public static IServiceCollection AddHelixCharts(this IServiceCollection services) =>
        services.AddApexCharts();
}

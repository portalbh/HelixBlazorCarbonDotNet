namespace HelixCarbon.Shared.DTOs;

public sealed record DashboardMetricsDto(
    IReadOnlyList<string> Labels,
    IReadOnlyList<decimal> Revenue,
    IReadOnlyList<int> ProductCounts);

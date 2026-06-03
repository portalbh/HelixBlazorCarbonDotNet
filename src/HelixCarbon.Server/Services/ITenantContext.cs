using HelixCarbon.Shared.Enums;
using HelixCarbon.Shared.Models;

namespace HelixCarbon.Server.Services;

public interface ITenantContext
{
    Guid? TenantId { get; }
    Tenant? Tenant { get; }
    bool IsResolved { get; }
    void SetTenant(Tenant tenant);
}

public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId => Tenant?.Id;
    public Tenant? Tenant { get; private set; }
    public bool IsResolved => Tenant is not null;

    public void SetTenant(Tenant tenant) => Tenant = tenant;
}

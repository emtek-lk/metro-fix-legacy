using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Tenant-scoped service request lifecycle transition rule.
/// </summary>
public sealed class ServiceRequestLifecycleTransition
{
    public ServiceRequestLifecycleTransition(Guid id, Guid tenantId, ServiceRequestStatus fromStatus, ServiceRequestStatus toStatus)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "Lifecycle transition id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "Lifecycle transition must belong to a tenant.");
        this.FromStatus = fromStatus;
        this.ToStatus = toStatus;
        this.IsEnabled = true;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public ServiceRequestStatus FromStatus { get; private set; }

    public ServiceRequestStatus ToStatus { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public void Disable()
    {
        this.IsEnabled = false;
    }
}
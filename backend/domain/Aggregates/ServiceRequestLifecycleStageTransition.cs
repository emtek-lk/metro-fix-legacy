using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Tenant-scoped lifecycle transition between configured request stages.
/// </summary>
public sealed class ServiceRequestLifecycleStageTransition
{
    public ServiceRequestLifecycleStageTransition(Guid id, Guid tenantId, Guid fromStageId, Guid toStageId)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "Lifecycle stage transition id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "Lifecycle stage transition must belong to a tenant.");
        this.FromStageId = DomainGuards.RequiredId(fromStageId, nameof(fromStageId), "fromStageId is required.");
        this.ToStageId = DomainGuards.RequiredId(toStageId, nameof(toStageId), "toStageId is required.");
        this.IsEnabled = true;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public Guid FromStageId { get; private set; }

    public Guid ToStageId { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public bool IsDeleted { get; internal set; }
}

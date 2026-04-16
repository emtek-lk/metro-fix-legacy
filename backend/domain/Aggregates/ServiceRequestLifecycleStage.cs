using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Tenant-scoped lifecycle stage definition for service requests.
/// </summary>
public sealed class ServiceRequestLifecycleStage
{
    public ServiceRequestLifecycleStage(
        Guid id,
        Guid tenantId,
        string statusCode,
        string displayName,
        int displayOrder)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "Lifecycle stage id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "Lifecycle stage must belong to a tenant.");
        this.StatusCode = DomainGuards.RequiredText(statusCode, nameof(statusCode), "Lifecycle stage status code is required.", 32);
        this.DisplayName = DomainGuards.RequiredText(displayName, nameof(displayName), "Lifecycle stage display name is required.", 120);
        this.DisplayOrder = displayOrder;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public string StatusCode { get; private set; }

    public string DisplayName { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public bool IsDeleted { get; internal set; }
}

using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface IServiceRequestLifecycleTransitionRepository : IRepository<ServiceRequestLifecycleTransition>
{
    Task<bool> HasConfiguredTransitionsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<bool> IsEnabledTransitionAsync(
        Guid tenantId,
        ServiceRequestStatus fromStatus,
        ServiceRequestStatus toStatus,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequestLifecycleTransition>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task DeleteByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
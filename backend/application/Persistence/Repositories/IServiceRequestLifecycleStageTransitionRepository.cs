using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface IServiceRequestLifecycleStageTransitionRepository : IRepository<ServiceRequestLifecycleStageTransition>
{
    Task<IReadOnlyList<ServiceRequestLifecycleStageTransition>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task DeleteByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

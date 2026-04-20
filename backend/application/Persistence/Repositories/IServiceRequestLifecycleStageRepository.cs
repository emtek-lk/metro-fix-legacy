using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface IServiceRequestLifecycleStageRepository : IRepository<ServiceRequestLifecycleStage>
{
    Task<IReadOnlyList<ServiceRequestLifecycleStage>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task DeleteByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

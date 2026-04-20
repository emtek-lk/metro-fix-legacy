using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class ServiceRequestLifecycleStageTransitionRepository : EfRepository<ServiceRequestLifecycleStageTransition>, IServiceRequestLifecycleStageTransitionRepository
{
    public ServiceRequestLifecycleStageTransitionRepository(GtekFsmDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<ServiceRequestLifecycleStageTransition>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await this.Queryable().AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.FromStageId)
            .ThenBy(x => x.ToStageId)
            .ToListAsync(cancellationToken);
    }

    public Task DeleteByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return this.Queryable()
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

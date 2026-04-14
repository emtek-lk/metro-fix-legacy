using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class ServiceRequestLifecycleTransitionRepository : EfRepository<ServiceRequestLifecycleTransition>, IServiceRequestLifecycleTransitionRepository
{
    public ServiceRequestLifecycleTransitionRepository(GtekFsmDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<bool> HasConfiguredTransitionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return this.Queryable().AsNoTracking().AnyAsync(x => x.TenantId == tenantId, cancellationToken);
    }

    public Task<bool> IsEnabledTransitionAsync(
        Guid tenantId,
        ServiceRequestStatus fromStatus,
        ServiceRequestStatus toStatus,
        CancellationToken cancellationToken = default)
    {
        return this.Queryable().AsNoTracking().AnyAsync(
            x => x.TenantId == tenantId
                && x.FromStatus == fromStatus
                && x.ToStatus == toStatus
                && x.IsEnabled,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequestLifecycleTransition>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await this.Queryable().AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.FromStatus)
            .ThenBy(x => x.ToStatus)
            .ToListAsync(cancellationToken);
    }

    public Task DeleteByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return this.Queryable()
            .Where(x => x.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
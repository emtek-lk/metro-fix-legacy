using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Audit;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Policies;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

internal sealed class ServiceRequestLifecycleDefinitionService : IServiceRequestLifecycleDefinitionService
{
    private readonly IServiceRequestLifecycleTransitionRepository transitionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IAuditLogWriter auditLogWriter;

    public ServiceRequestLifecycleDefinitionService(
        IServiceRequestLifecycleTransitionRepository transitionRepository,
        IUnitOfWork unitOfWork,
        IAuditLogWriter auditLogWriter)
    {
        this.transitionRepository = transitionRepository;
        this.unitOfWork = unitOfWork;
        this.auditLogWriter = auditLogWriter;
    }

    public async Task<ServiceRequestLifecycleDefinitionResult> GetAsync(
        AuthenticatedPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return ServiceRequestLifecycleDefinitionResult.Failure(
                "Role is not authorized to manage request lifecycle.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var transitions = await EnsureTransitionsAsync(principal.TenantId, cancellationToken);
        return ServiceRequestLifecycleDefinitionResult.Success(transitions, "Request lifecycle retrieved.");
    }

    public async Task<ServiceRequestLifecycleDefinitionResult> ReplaceAsync(
        AuthenticatedPrincipal principal,
        UpdateServiceRequestLifecycleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return ServiceRequestLifecycleDefinitionResult.Failure(
                "Role is not authorized to manage request lifecycle.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var items = request.Items?.ToArray() ?? Array.Empty<UpdateServiceRequestLifecycleTransitionRequest>();
        if (items.Length == 0)
        {
            return ServiceRequestLifecycleDefinitionResult.Failure(
                "At least one lifecycle transition is required.",
                "LIFECYCLE_TRANSITIONS_REQUIRED",
                400);
        }

        var parsedTransitions = new HashSet<(ServiceRequestStatus From, ServiceRequestStatus To)>();
        foreach (var item in items)
        {
            if (!TryParseStatus(item.FromStatus, out var fromStatus) || !TryParseStatus(item.ToStatus, out var toStatus))
            {
                return ServiceRequestLifecycleDefinitionResult.Failure(
                    "Lifecycle transitions contain invalid statuses.",
                    "LIFECYCLE_STATUS_INVALID",
                    400);
            }

            parsedTransitions.Add((fromStatus, toStatus));
        }

        await this.transitionRepository.DeleteByTenantAsync(principal.TenantId, cancellationToken);
        foreach (var transition in parsedTransitions)
        {
            var rule = new ServiceRequestLifecycleTransition(
                Guid.NewGuid(),
                principal.TenantId,
                transition.From,
                transition.To);

            await this.transitionRepository.AddAsync(rule, cancellationToken);
        }

        await this.unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(principal, "REQUEST_LIFECYCLE_UPDATED", cancellationToken);

        var payload = await BuildPayloadAsync(principal.TenantId, cancellationToken);
        return ServiceRequestLifecycleDefinitionResult.Success(payload, "Request lifecycle updated.");
    }

    private async Task<IReadOnlyList<QueriedServiceRequestLifecycleTransition>> EnsureTransitionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (await this.transitionRepository.HasConfiguredTransitionsAsync(tenantId, cancellationToken))
        {
            return await BuildPayloadAsync(tenantId, cancellationToken);
        }

        foreach (var transition in ServiceRequestStateTransitions.GetDefaultTransitions())
        {
            await this.transitionRepository.AddAsync(
                new ServiceRequestLifecycleTransition(Guid.NewGuid(), tenantId, transition.From, transition.To),
                cancellationToken);
        }

        await this.unitOfWork.SaveChangesAsync(cancellationToken);
        return await BuildPayloadAsync(tenantId, cancellationToken);
    }

    private async Task<IReadOnlyList<QueriedServiceRequestLifecycleTransition>> BuildPayloadAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var transitions = await this.transitionRepository.ListByTenantAsync(tenantId, cancellationToken);
        return transitions
            .Select(x => new QueriedServiceRequestLifecycleTransition(
                FromStatus: x.FromStatus.ToString(),
                ToStatus: x.ToStatus.ToString(),
                IsEnabled: x.IsEnabled))
            .ToArray();
    }

    private async Task WriteAuditAsync(
        AuthenticatedPrincipal principal,
        string action,
        CancellationToken cancellationToken)
    {
        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = principal.UserId,
            TenantId = principal.TenantId,
            EntityType = "ServiceRequestLifecycle",
            EntityId = Guid.Empty,
            Action = action,
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = null,
        };

        await this.auditLogWriter.WriteAsync(audit, cancellationToken);
    }

    private static bool TryParseStatus(string? value, out ServiceRequestStatus status)
    {
        return Enum.TryParse<ServiceRequestStatus>(value?.Trim(), ignoreCase: true, out status);
    }

    private static bool IsManagementRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Manager") || principal.IsInRole("Admin");
    }
}
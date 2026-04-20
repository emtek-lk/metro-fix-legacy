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
    private static readonly string[] DefaultStatuses =
    [
        ServiceRequestStatus.New.ToString(),
        ServiceRequestStatus.Assigned.ToString(),
        ServiceRequestStatus.InProgress.ToString(),
        ServiceRequestStatus.OnHold.ToString(),
        ServiceRequestStatus.Completed.ToString(),
        ServiceRequestStatus.Cancelled.ToString(),
    ];

    private static readonly (string From, string To)[] DefaultTransitions =
    [
        ("New", "Assigned"),
        ("New", "Cancelled"),
        ("Assigned", "InProgress"),
        ("Assigned", "OnHold"),
        ("Assigned", "Cancelled"),
        ("InProgress", "OnHold"),
        ("InProgress", "Completed"),
        ("InProgress", "Cancelled"),
        ("OnHold", "Assigned"),
        ("OnHold", "InProgress"),
        ("OnHold", "Cancelled"),
    ];

    private readonly IServiceRequestLifecycleTransitionRepository transitionRepository;
    private readonly IServiceRequestLifecycleStageRepository stageRepository;
    private readonly IServiceRequestLifecycleStageTransitionRepository stageTransitionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IAuditLogWriter auditLogWriter;

    public ServiceRequestLifecycleDefinitionService(
        IServiceRequestLifecycleTransitionRepository transitionRepository,
        IServiceRequestLifecycleStageRepository stageRepository,
        IServiceRequestLifecycleStageTransitionRepository stageTransitionRepository,
        IUnitOfWork unitOfWork,
        IAuditLogWriter auditLogWriter)
    {
        this.transitionRepository = transitionRepository;
        this.stageRepository = stageRepository;
        this.stageTransitionRepository = stageTransitionRepository;
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

        var definition = await EnsureDefinitionAsync(principal.TenantId, cancellationToken);
        return ServiceRequestLifecycleDefinitionResult.Success(definition, "Request lifecycle retrieved.");
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

        var stageRequests = request.Stages?.ToArray() ?? Array.Empty<UpdateServiceRequestLifecycleStageRequest>();
        if (stageRequests.Length == 0)
        {
            return ServiceRequestLifecycleDefinitionResult.Failure(
                "At least one lifecycle stage is required.",
                "LIFECYCLE_STAGES_REQUIRED",
                400);
        }

        var parsedStages = new List<ServiceRequestLifecycleStage>(stageRequests.Length);
        var stageById = new Dictionary<Guid, ServiceRequestLifecycleStage>();
        var seenDisplayNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var stageRequest in stageRequests)
        {
            if (!TryParseStatus(stageRequest.StatusCode, out var parsedStatus))
            {
                return ServiceRequestLifecycleDefinitionResult.Failure(
                    "Lifecycle stages contain invalid status codes.",
                    "LIFECYCLE_STAGE_STATUS_INVALID",
                    400);
            }

            var stageId = TryParseGuid(stageRequest.StageId, out var parsedStageId) ? parsedStageId : Guid.NewGuid();
            if (stageById.ContainsKey(stageId))
            {
                return ServiceRequestLifecycleDefinitionResult.Failure(
                    "Lifecycle stages contain duplicate stage ids.",
                    "LIFECYCLE_STAGE_DUPLICATE_ID",
                    400);
            }

            var displayName = stageRequest.DisplayName?.Trim();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return ServiceRequestLifecycleDefinitionResult.Failure(
                    "Lifecycle stage displayName is required.",
                    "LIFECYCLE_STAGE_DISPLAY_NAME_REQUIRED",
                    400);
            }

            if (!seenDisplayNames.Add(displayName))
            {
                return ServiceRequestLifecycleDefinitionResult.Failure(
                    "Lifecycle stage display names must be unique.",
                    "LIFECYCLE_STAGE_DISPLAY_NAME_DUPLICATE",
                    400);
            }

            var stage = new ServiceRequestLifecycleStage(
                stageId,
                principal.TenantId,
                parsedStatus.ToString(),
                displayName,
                stageRequest.DisplayOrder);

            stageById.Add(stageId, stage);
            parsedStages.Add(stage);
        }

        var parsedTransitions = new HashSet<(Guid FromStageId, Guid ToStageId)>();
        foreach (var item in items)
        {
            if (!TryResolveStageId(item.FromStageId, item.FromStatus, stageById, out var fromStageId)
                || !TryResolveStageId(item.ToStageId, item.ToStatus, stageById, out var toStageId))
            {
                return ServiceRequestLifecycleDefinitionResult.Failure(
                    "Lifecycle transitions contain unknown stages.",
                    "LIFECYCLE_STAGE_REFERENCE_INVALID",
                    400);
            }

            if (fromStageId == toStageId)
            {
                continue;
            }

            parsedTransitions.Add((fromStageId, toStageId));
        }

        if (parsedTransitions.Count == 0)
        {
            return ServiceRequestLifecycleDefinitionResult.Failure(
                "At least one lifecycle transition is required.",
                "LIFECYCLE_TRANSITIONS_REQUIRED",
                400);
        }

        await this.transitionRepository.DeleteByTenantAsync(principal.TenantId, cancellationToken);
        await this.stageTransitionRepository.DeleteByTenantAsync(principal.TenantId, cancellationToken);
        await this.stageRepository.DeleteByTenantAsync(principal.TenantId, cancellationToken);

        foreach (var stage in parsedStages.OrderBy(x => x.DisplayOrder))
        {
            await this.stageRepository.AddAsync(stage, cancellationToken);
        }

        foreach (var transition in parsedTransitions)
        {
            await this.stageTransitionRepository.AddAsync(
                new ServiceRequestLifecycleStageTransition(Guid.NewGuid(), principal.TenantId, transition.FromStageId, transition.ToStageId),
                cancellationToken);
        }

        var effectiveTransitions = new HashSet<(ServiceRequestStatus From, ServiceRequestStatus To)>();
        foreach (var transition in parsedTransitions)
        {
            var fromStage = stageById[transition.FromStageId];
            var toStage = stageById[transition.ToStageId];
            _ = TryParseStatus(fromStage.StatusCode, out var fromStatus);
            _ = TryParseStatus(toStage.StatusCode, out var toStatus);
            effectiveTransitions.Add((fromStatus, toStatus));
        }

        foreach (var transition in effectiveTransitions)
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

    private async Task<QueriedServiceRequestLifecycleDefinition> EnsureDefinitionAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var configuredStages = await this.stageRepository.ListByTenantAsync(tenantId, cancellationToken);
        if (configuredStages.Count != 0)
        {
            return await BuildPayloadAsync(tenantId, cancellationToken);
        }

        var stages = DefaultStatuses
            .Select((statusCode, index) => new ServiceRequestLifecycleStage(Guid.NewGuid(), tenantId, statusCode, statusCode, index))
            .ToArray();

        var stageByStatus = stages.ToDictionary(x => x.StatusCode, StringComparer.OrdinalIgnoreCase);

        foreach (var stage in stages)
        {
            await this.stageRepository.AddAsync(stage, cancellationToken);
        }

        foreach (var transition in DefaultTransitions)
        {
            await this.stageTransitionRepository.AddAsync(
                new ServiceRequestLifecycleStageTransition(
                    Guid.NewGuid(),
                    tenantId,
                    stageByStatus[transition.From].Id,
                    stageByStatus[transition.To].Id),
                cancellationToken);
        }

        // Keep legacy status transitions in sync with seeded stage graph.
        // Existing tenants may already have rows here from pre-stage lifecycle support.
        await this.transitionRepository.DeleteByTenantAsync(tenantId, cancellationToken);

        foreach (var transition in ServiceRequestStateTransitions.GetDefaultTransitions())
        {
            await this.transitionRepository.AddAsync(
                new ServiceRequestLifecycleTransition(Guid.NewGuid(), tenantId, transition.From, transition.To),
                cancellationToken);
        }

        await this.unitOfWork.SaveChangesAsync(cancellationToken);
        return await BuildPayloadAsync(tenantId, cancellationToken);
    }

    private async Task<QueriedServiceRequestLifecycleDefinition> BuildPayloadAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var stages = await this.stageRepository.ListByTenantAsync(tenantId, cancellationToken);
        var stageTransitions = await this.stageTransitionRepository.ListByTenantAsync(tenantId, cancellationToken);

        var stageDtos = stages
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new QueriedServiceRequestLifecycleStage(
                StageId: x.Id,
                StatusCode: x.StatusCode,
                DisplayName: x.DisplayName,
                DisplayOrder: x.DisplayOrder))
            .ToArray();

        var stageById = stageDtos.ToDictionary(x => x.StageId);
        var transitionDtos = stageTransitions
            .Where(x => stageById.ContainsKey(x.FromStageId) && stageById.ContainsKey(x.ToStageId))
            .OrderBy(x => stageById[x.FromStageId].DisplayOrder)
            .ThenBy(x => stageById[x.ToStageId].DisplayOrder)
            .Select(x => new QueriedServiceRequestLifecycleTransition(
                FromStageId: x.FromStageId,
                ToStageId: x.ToStageId,
                FromStatus: stageById[x.FromStageId].DisplayName,
                ToStatus: stageById[x.ToStageId].DisplayName,
                IsEnabled: x.IsEnabled))
            .ToArray();

        return new QueriedServiceRequestLifecycleDefinition(stageDtos, transitionDtos);
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

    private static bool TryResolveStageId(
        string? stageId,
        string? fallbackStatus,
        IReadOnlyDictionary<Guid, ServiceRequestLifecycleStage> stageById,
        out Guid resolvedStageId)
    {
        if (TryParseGuid(stageId, out var parsed) && stageById.ContainsKey(parsed))
        {
            resolvedStageId = parsed;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(fallbackStatus))
        {
            var match = stageById.Values.FirstOrDefault(x =>
                string.Equals(x.DisplayName, fallbackStatus.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.StatusCode, fallbackStatus.Trim(), StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                resolvedStageId = match.Id;
                return true;
            }
        }

        resolvedStageId = Guid.Empty;
        return false;
    }

    private static bool TryParseGuid(string? value, out Guid parsed)
    {
        return Guid.TryParse(value?.Trim(), out parsed) && parsed != Guid.Empty;
    }

    private static bool IsManagementRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Manager") || principal.IsInRole("Admin");
    }
}
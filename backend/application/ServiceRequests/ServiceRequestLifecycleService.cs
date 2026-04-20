using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Application.Realtime;
using GTEK.FSM.Backend.Domain.Audit;
using GTEK.FSM.Backend.Domain.Enums;
using System.Text.Json;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

internal sealed class ServiceRequestLifecycleService : IServiceRequestLifecycleService
{
    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IAuditLogWriter auditLogWriter;
    private readonly IOperationalUpdatePublisher operationalUpdatePublisher;
    private readonly IServiceRequestLifecycleStageRepository stageRepository;
    private readonly IServiceRequestLifecycleStageTransitionRepository stageTransitionRepository;
    private readonly ServiceRequestSlaOptions slaOptions = new();

    public ServiceRequestLifecycleService(
        IServiceRequestRepository serviceRequestRepository,
        IUnitOfWork unitOfWork,
        IAuditLogWriter auditLogWriter,
        IOperationalUpdatePublisher operationalUpdatePublisher,
        IServiceRequestLifecycleStageRepository stageRepository,
        IServiceRequestLifecycleStageTransitionRepository stageTransitionRepository)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.unitOfWork = unitOfWork;
        this.auditLogWriter = auditLogWriter;
        this.operationalUpdatePublisher = operationalUpdatePublisher;
        this.stageRepository = stageRepository;
        this.stageTransitionRepository = stageTransitionRepository;
    }

    public async Task<TransitionServiceRequestResult> TransitionAsync(
        AuthenticatedPrincipal principal,
        Guid requestId,
        string? nextStageId,
        string? nextStatus,
        string? rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nextStatus) && string.IsNullOrWhiteSpace(nextStageId))
        {
            return TransitionServiceRequestResult.Failure(
                message: "Next status or nextStageId is required.",
                errorCode: "VALIDATION_NEXT_TARGET_REQUIRED",
                statusCode: 400);
        }

        var request = await this.serviceRequestRepository.GetForUpdateAsync(principal.TenantId, requestId, cancellationToken);
        if (request is null)
        {
            return TransitionServiceRequestResult.Failure(
                message: "Service request was not found.",
                errorCode: "REQUEST_NOT_FOUND",
                statusCode: 404);
        }

        if (!TryValidateRowVersion(rowVersion, request.RowVersion, out var validationErrorCode, out var validationMessage))
        {
            return TransitionServiceRequestResult.Failure(
                message: validationMessage,
                errorCode: validationErrorCode,
                statusCode: 409);
        }

        var configuredStages = await this.stageRepository.ListByTenantAsync(principal.TenantId, cancellationToken);
        var configuredStageTransitions = await this.stageTransitionRepository.ListByTenantAsync(principal.TenantId, cancellationToken);

        ServiceRequestStatus parsedNextStatus;
        Guid? resolvedCurrentStageId = request.CurrentStageId;
        Guid? resolvedNextStageId = null;

        if (configuredStages.Count == 0 || configuredStageTransitions.Count == 0)
        {
            return TransitionServiceRequestResult.Failure(
                message: "Request lifecycle is not configured for this tenant.",
                errorCode: "REQUEST_LIFECYCLE_NOT_CONFIGURED",
                statusCode: 400);
        }

        {
            if (!resolvedCurrentStageId.HasValue
                || configuredStages.All(x => x.Id != resolvedCurrentStageId.Value))
            {
                resolvedCurrentStageId = configuredStages
                    .Where(x => string.Equals(x.StatusCode, request.Status.ToString(), StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefault();
            }

            if (Guid.TryParse(nextStageId?.Trim(), out var parsedNextStageId) && parsedNextStageId != Guid.Empty)
            {
                resolvedNextStageId = configuredStages.Any(x => x.Id == parsedNextStageId)
                    ? parsedNextStageId
                    : null;
            }

            if (!resolvedNextStageId.HasValue && !string.IsNullOrWhiteSpace(nextStatus))
            {
                resolvedNextStageId = configuredStages
                    .Where(x => string.Equals(x.StatusCode, nextStatus.Trim(), StringComparison.OrdinalIgnoreCase)
                        || string.Equals(x.DisplayName, nextStatus.Trim(), StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefault();
            }

            if (!resolvedCurrentStageId.HasValue || !resolvedNextStageId.HasValue)
            {
                return TransitionServiceRequestResult.Failure(
                    message: "Requested lifecycle stage is invalid.",
                    errorCode: "VALIDATION_NEXT_STAGE_INVALID",
                    statusCode: 400);
            }

            var nextStage = configuredStages.First(x => x.Id == resolvedNextStageId.Value);
            if (!Enum.TryParse<ServiceRequestStatus>(nextStage.StatusCode.Trim(), ignoreCase: true, out parsedNextStatus))
            {
                return TransitionServiceRequestResult.Failure(
                    message: "Requested stage is not mapped to a valid service request status.",
                    errorCode: "VALIDATION_NEXT_STATUS_INVALID",
                    statusCode: 400);
            }

            var stageTransitionAllowed = configuredStageTransitions.Any(x =>
                x.FromStageId == resolvedCurrentStageId.Value
                && x.ToStageId == resolvedNextStageId.Value
                && x.IsEnabled);

            if (!stageTransitionAllowed)
            {
                return TransitionServiceRequestResult.Failure(
                    message: "Requested lifecycle stage transition is not allowed.",
                    errorCode: "REQUEST_TRANSITION_INVALID",
                    statusCode: 400);
            }
        }

        // Treat duplicate writes to the current status as idempotent success.
        if (request.Status == parsedNextStatus
            && (!resolvedNextStageId.HasValue || request.CurrentStageId == resolvedNextStageId))
        {
            var duplicatePayload = new TransitionedServiceRequestPayload(
                RequestId: request.Id,
                TenantId: request.TenantId,
                PreviousStatus: request.Status.ToString(),
                CurrentStatus: request.Status.ToString(),
                UpdatedAtUtc: request.UpdatedAtUtc,
                RowVersion: Convert.ToBase64String(request.RowVersion));

            return TransitionServiceRequestResult.Success(duplicatePayload);
        }

        var previousStatus = request.Status;
        var previousResponseSlaState = request.ResponseSlaState;
        var previousAssignmentSlaState = request.AssignmentSlaState;
        var previousCompletionSlaState = request.CompletionSlaState;

        try
        {
            request.TransitionTo(parsedNextStatus);
            if (resolvedNextStageId.HasValue)
            {
                request.SetCurrentStage(resolvedNextStageId.Value);
            }
        }
        catch (InvalidOperationException ex)
        {
            return TransitionServiceRequestResult.Failure(
                message: ex.Message,
                errorCode: "REQUEST_TRANSITION_INVALID",
                statusCode: 400);
        }

        var snapshot = ServiceRequestSlaCalculator.Compute(
            request,
            assignmentStatus: null,
            nowUtc: DateTime.UtcNow,
            options: this.slaOptions);

        var escalations = ServiceRequestSlaEscalationEvaluator.Evaluate(
            previousResponseSlaState,
            previousAssignmentSlaState,
            previousCompletionSlaState,
            snapshot);

        request.ApplySlaSnapshot(
            snapshot.ResponseDueAtUtc,
            snapshot.AssignmentDueAtUtc,
            snapshot.CompletionDueAtUtc,
            snapshot.ResponseSlaState,
            snapshot.AssignmentSlaState,
            snapshot.CompletionSlaState,
            snapshot.NextSlaDeadlineAtUtc);

        this.serviceRequestRepository.Update(request);
        try
        {
            await this.unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return TransitionServiceRequestResult.Failure(
                message: "The request was modified by another operation. Refresh and retry.",
                errorCode: "CONCURRENCY_CONFLICT",
                statusCode: 409);
        }

        // Write audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = principal.UserId,
            TenantId = principal.TenantId,
            EntityType = "ServiceRequest",
            EntityId = request.Id,
            Action = $"StatusTransition:{previousStatus}->{request.Status}",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = null,
        };
        await this.auditLogWriter.WriteAsync(auditLog, cancellationToken);

        foreach (var escalation in escalations)
        {
            var escalationAudit = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = null,
                TenantId = principal.TenantId,
                EntityType = "ServiceRequest",
                EntityId = request.Id,
                Action = $"SlaEscalation:{escalation.SlaDimension}:{escalation.CurrentState}",
                Outcome = "Success",
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Details = JsonSerializer.Serialize(new
                {
                    escalation.SlaDimension,
                    PreviousState = escalation.PreviousState.ToString(),
                    CurrentState = escalation.CurrentState.ToString(),
                    escalation.DueAtUtc,
                    TriggeredByUserId = principal.UserId,
                }),
            };

            await this.auditLogWriter.WriteAsync(escalationAudit, cancellationToken);

            var escalationPayload = new SlaEscalationTriggeredPayload(
                RequestId: request.Id,
                TenantId: request.TenantId,
                SlaDimension: escalation.SlaDimension,
                PreviousSlaStatus: escalation.PreviousState.ToString(),
                CurrentSlaStatus: escalation.CurrentState.ToString(),
                DueAtUtc: escalation.DueAtUtc,
                TriggeredAtUtc: DateTime.UtcNow,
                RowVersion: Convert.ToBase64String(request.RowVersion));

            await this.operationalUpdatePublisher.PublishSlaEscalationTriggeredAsync(escalationPayload, cancellationToken);
        }

        var payload = new TransitionedServiceRequestPayload(
            RequestId: request.Id,
            TenantId: request.TenantId,
            PreviousStatus: previousStatus.ToString(),
            CurrentStatus: request.Status.ToString(),
            UpdatedAtUtc: request.UpdatedAtUtc,
            RowVersion: Convert.ToBase64String(request.RowVersion));

        await this.operationalUpdatePublisher.PublishServiceRequestStatusUpdatedAsync(payload, cancellationToken);

        return TransitionServiceRequestResult.Success(payload);
    }

    private static bool TryValidateRowVersion(
        string? requestRowVersion,
        byte[] currentRowVersion,
        out string errorCode,
        out string message)
    {
        errorCode = string.Empty;
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(requestRowVersion))
        {
            return true;
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(requestRowVersion.Trim());
        }
        catch (FormatException)
        {
            errorCode = "ROW_VERSION_INVALID";
            message = "rowVersion must be a valid base64 string.";
            return false;
        }

        if (!decoded.AsSpan().SequenceEqual(currentRowVersion))
        {
            errorCode = "CONCURRENCY_CONFLICT";
            message = "The request was modified by another operation. Refresh and retry.";
            return false;
        }

        return true;
    }
}

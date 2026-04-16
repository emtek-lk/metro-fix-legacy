using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

internal sealed class ServiceRequestCreationService : IServiceRequestCreationService
{
    private const int MaxTitleLength = 180;

    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IServiceRequestLifecycleStageRepository stageRepository;
    private readonly ServiceRequestSlaOptions slaOptions = new();

    public ServiceRequestCreationService(
        IServiceRequestRepository serviceRequestRepository,
        IServiceRequestLifecycleStageRepository stageRepository,
        IUnitOfWork unitOfWork)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.stageRepository = stageRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateServiceRequestResult> CreateAsync(AuthenticatedPrincipal principal, string? title, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return CreateServiceRequestResult.ValidationFailure(
                message: "Request title is required.",
                errorCode: "VALIDATION_TITLE_REQUIRED");
        }

        if (normalizedTitle.Length > MaxTitleLength)
        {
            return CreateServiceRequestResult.ValidationFailure(
                message: $"Request title exceeds maximum length of {MaxTitleLength} characters.",
                errorCode: "VALIDATION_TITLE_TOO_LONG");
        }

        var request = new ServiceRequest(
            id: Guid.NewGuid(),
            tenantId: principal.TenantId,
            customerUserId: principal.UserId,
            title: normalizedTitle);

        var stages = await this.stageRepository.ListByTenantAsync(principal.TenantId, cancellationToken);
        var defaultStage = stages
            .Where(x => string.Equals(x.StatusCode, ServiceRequestStatus.New.ToString(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DisplayOrder)
            .FirstOrDefault();

        if (defaultStage is not null)
        {
            request.SetCurrentStage(defaultStage.Id);
        }

        var snapshot = ServiceRequestSlaCalculator.Compute(
            request,
            assignmentStatus: null,
            nowUtc: DateTime.UtcNow,
            options: this.slaOptions);

        request.ApplySlaSnapshot(
            snapshot.ResponseDueAtUtc,
            snapshot.AssignmentDueAtUtc,
            snapshot.CompletionDueAtUtc,
            snapshot.ResponseSlaState,
            snapshot.AssignmentSlaState,
            snapshot.CompletionSlaState,
            snapshot.NextSlaDeadlineAtUtc);

        await this.serviceRequestRepository.AddAsync(request, cancellationToken);
        await this.unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateServiceRequestResult.Success(
            new CreatedServiceRequestPayload(
                RequestId: request.Id,
                TenantId: request.TenantId,
                CustomerUserId: request.CustomerUserId,
                Title: request.Title,
                Status: request.Status.ToString(),
                CreatedAtUtc: request.CreatedAtUtc,
                UpdatedAtUtc: request.UpdatedAtUtc));
    }
}

using FluentValidation;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class TransitionServiceRequestStatusRequestValidator : AbstractValidator<TransitionServiceRequestStatusRequest>
{
    public TransitionServiceRequestStatusRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.NextStatus) || !string.IsNullOrWhiteSpace(x.NextStageId))
            .WithMessage("nextStatus or nextStageId is required.");

        RuleFor(x => x.NextStageId)
            .Must(BeValidGuidOrEmpty)
            .WithMessage("nextStageId must be a valid guid when provided.");

        RuleFor(x => x.NextStatus)
            .Must(BeValidServiceRequestStatusOrEmpty)
            .WithMessage("Requested status is invalid.");
    }

    private static bool BeValidServiceRequestStatus(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<ServiceRequestStatus>(value.Trim(), ignoreCase: true, out _);
    }

    private static bool BeValidServiceRequestStatusOrEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || BeValidServiceRequestStatus(value);
    }

    private static bool BeValidGuidOrEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || Guid.TryParse(value.Trim(), out var parsed) && parsed != Guid.Empty;
    }
}

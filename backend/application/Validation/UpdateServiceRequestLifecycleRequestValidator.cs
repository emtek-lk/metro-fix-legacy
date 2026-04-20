using FluentValidation;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class UpdateServiceRequestLifecycleRequestValidator : AbstractValidator<UpdateServiceRequestLifecycleRequest>
{
    public UpdateServiceRequestLifecycleRequestValidator()
    {
        RuleFor(x => x.Stages)
            .NotEmpty()
            .WithMessage("stages is required.");

        RuleForEach(x => x.Stages)
            .ChildRules(stage =>
            {
                stage.RuleFor(i => i.StageId)
                    .Must(BeGuidOrEmpty)
                    .WithMessage("stageId must be a valid guid when provided.");

                stage.RuleFor(i => i.StatusCode)
                    .NotEmpty()
                    .WithMessage("statusCode is required.")
                    .Must(BeServiceRequestStatus)
                    .WithMessage("statusCode must be a valid service request status.");

                stage.RuleFor(i => i.DisplayName)
                    .NotEmpty()
                    .WithMessage("displayName is required.")
                    .MaximumLength(120)
                    .WithMessage("displayName cannot exceed 120 characters.");

                stage.RuleFor(i => i.DisplayOrder)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("displayOrder must be zero or greater.");
            });

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("items is required.");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.FromStatus)
                    .Must(BeServiceRequestStatusOrEmpty)
                    .WithMessage("fromStatus must be a valid service request status when provided.");

                item.RuleFor(i => i.FromStageId)
                    .Must(BeGuidOrEmpty)
                    .WithMessage("fromStageId must be a valid guid when provided.");

                item.RuleFor(i => i)
                    .Must(i => !string.IsNullOrWhiteSpace(i.FromStatus) || !string.IsNullOrWhiteSpace(i.FromStageId))
                    .WithMessage("fromStageId or fromStatus is required.");

                item.RuleFor(i => i.ToStatus)
                    .Must(BeServiceRequestStatusOrEmpty)
                    .WithMessage("toStatus must be a valid service request status when provided.");

                item.RuleFor(i => i.ToStageId)
                    .Must(BeGuidOrEmpty)
                    .WithMessage("toStageId must be a valid guid when provided.");

                item.RuleFor(i => i)
                    .Must(i => !string.IsNullOrWhiteSpace(i.ToStatus) || !string.IsNullOrWhiteSpace(i.ToStageId))
                    .WithMessage("toStageId or toStatus is required.");
            });
    }

    private static bool BeServiceRequestStatus(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<ServiceRequestStatus>(value.Trim(), ignoreCase: true, out _);
    }

    private static bool BeServiceRequestStatusOrEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || BeServiceRequestStatus(value);
    }

    private static bool BeGuidOrEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || Guid.TryParse(value.Trim(), out var parsed) && parsed != Guid.Empty;
    }
}
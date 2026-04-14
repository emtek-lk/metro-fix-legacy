using FluentValidation;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class UpdateServiceRequestLifecycleRequestValidator : AbstractValidator<UpdateServiceRequestLifecycleRequest>
{
    public UpdateServiceRequestLifecycleRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("items is required.");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.FromStatus)
                    .NotEmpty()
                    .WithMessage("fromStatus is required.")
                    .Must(BeServiceRequestStatus)
                    .WithMessage("fromStatus must be a valid service request status.");

                item.RuleFor(i => i.ToStatus)
                    .NotEmpty()
                    .WithMessage("toStatus is required.")
                    .Must(BeServiceRequestStatus)
                    .WithMessage("toStatus must be a valid service request status.");
            });
    }

    private static bool BeServiceRequestStatus(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<ServiceRequestStatus>(value.Trim(), ignoreCase: true, out _);
    }
}
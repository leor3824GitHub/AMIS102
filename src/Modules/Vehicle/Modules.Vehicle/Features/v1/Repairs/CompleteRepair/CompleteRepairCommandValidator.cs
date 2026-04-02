using FluentValidation;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.CompleteRepair;

public sealed class CompleteRepairCommandValidator : AbstractValidator<CompleteRepairCommand>
{
    public CompleteRepairCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CompletedDate)
            .NotEmpty()
            .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow.AddDays(1));
    }
}

using FluentValidation;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.CreateRepairRecord;

public sealed class CreateRepairRecordCommandValidator : AbstractValidator<CreateRepairRecordCommand>
{
    public CreateRepairRecordCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.RepairDate).NotEmpty().LessThanOrEqualTo(_ => DateTimeOffset.UtcNow.AddDays(1));
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VendorName).MaximumLength(255).When(x => x.VendorName != null);
        RuleFor(x => x.VendorContact).MaximumLength(500).When(x => x.VendorContact != null);
        RuleFor(x => x.PartsUsed).MaximumLength(2000).When(x => x.PartsUsed != null);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}


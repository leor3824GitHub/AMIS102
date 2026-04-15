using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.AddFoundAtStationEntry;

public sealed class AddFoundAtStationEntryCommandValidator : AbstractValidator<AddFoundAtStationEntryCommand>
{
    public AddFoundAtStationEntryCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.PropertyNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Condition).IsInEnum();
        RuleFor(x => x.Remarks).MaximumLength(500).When(x => x.Remarks is not null);
        RuleFor(x => x.PhotoPath).MaximumLength(500).When(x => x.PhotoPath is not null);
    }
}

using FluentValidation;
using FSH.Modules.AssetManagement.Domain;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.RecordPhysicalCountEntry;

public sealed class RecordPhysicalCountEntryCommandValidator : AbstractValidator<RecordPhysicalCountEntryCommand>
{
    public RecordPhysicalCountEntryCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.EntryId).NotEmpty();
        RuleFor(x => x.Result).IsInEnum()
            .Must(r => r != PhysicalCountEntryResult.FoundAtStation)
            .WithMessage("Use the 'Found at Station' endpoint to add unrecorded assets.");
        RuleFor(x => x.QuantityOnHand).GreaterThan(0);
        RuleFor(x => x.PhotoPath).MaximumLength(500).When(x => x.PhotoPath is not null);

        // Condition is required when the asset was actually found
        RuleFor(x => x.Condition)
            .NotNull()
            .WithMessage("Condition is required when the asset is found.")
            .When(x => x.Result is PhysicalCountEntryResult.Found or PhysicalCountEntryResult.FoundAtStation);

        RuleFor(x => x.Condition)
            .IsInEnum()
            .When(x => x.Condition.HasValue);
    }
}

using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.RecordPhysicalCountEntry;

public sealed class RecordPhysicalCountEntryCommandValidator : AbstractValidator<RecordPhysicalCountEntryCommand>
{
    public RecordPhysicalCountEntryCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Physical count session ID is required.");
        RuleFor(x => x.AssetRegistryId).NotEmpty().WithMessage("Asset registry ID is required.");
        RuleFor(x => x.Article).NotEmpty().MaximumLength(256).WithMessage("Article must be provided and not exceed 256 characters.");
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(50).WithMessage("Unit must be provided and not exceed 50 characters.");
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).WithMessage("Unit cost must be non-negative.");
        RuleFor(x => x.LocationId).NotEmpty().WithMessage("Location ID is required.");
    }
}


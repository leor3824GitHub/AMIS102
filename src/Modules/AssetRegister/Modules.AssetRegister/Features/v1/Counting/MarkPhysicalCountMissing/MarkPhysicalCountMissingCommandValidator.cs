using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.MarkPhysicalCountMissing;

public sealed class MarkPhysicalCountMissingCommandValidator : AbstractValidator<MarkPhysicalCountMissingCommand>
{
    public MarkPhysicalCountMissingCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Physical count session ID is required.");
        RuleFor(x => x.AssetRegistryId).NotEmpty().WithMessage("Asset registry ID is required.");
        RuleFor(x => x.LocationId).NotEmpty().WithMessage("Location ID is required.");
    }
}


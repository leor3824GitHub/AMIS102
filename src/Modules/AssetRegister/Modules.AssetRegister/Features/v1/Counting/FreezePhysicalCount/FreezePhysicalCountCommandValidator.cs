using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.FreezePhysicalCount;

public sealed class FreezePhysicalCountCommandValidator : AbstractValidator<FreezePhysicalCountCommand>
{
    public FreezePhysicalCountCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Physical count session ID is required.");
        RuleFor(x => x.OfficeOrderNo)
            .NotEmpty().WithMessage("Office Order No. is required to freeze the ledger.")
            .MaximumLength(100);
    }
}

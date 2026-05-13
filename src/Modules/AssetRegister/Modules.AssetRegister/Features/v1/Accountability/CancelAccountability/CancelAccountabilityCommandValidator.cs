using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.CancelAccountability;

public sealed class CancelAccountabilityCommandValidator : AbstractValidator<CancelAccountabilityCommand>
{
    public CancelAccountabilityCommandValidator()
    {
        RuleFor(x => x.AccountabilityId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}


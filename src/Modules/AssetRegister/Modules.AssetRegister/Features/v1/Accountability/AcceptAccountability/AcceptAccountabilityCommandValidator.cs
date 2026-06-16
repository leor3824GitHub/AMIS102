using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.AcceptAccountability;

public sealed class AcceptAccountabilityCommandValidator : AbstractValidator<AcceptAccountabilityCommand>
{
    public AcceptAccountabilityCommandValidator()
    {
        RuleFor(x => x.AccountabilityId).NotEmpty();
        RuleFor(x => x.AcceptedOn).NotEmpty();
    }
}

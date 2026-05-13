using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.RenewAccountability;

public sealed class RenewAccountabilityCommandValidator : AbstractValidator<RenewAccountabilityCommand>
{
    public RenewAccountabilityCommandValidator()
    {
        RuleFor(x => x.AccountabilityId).NotEmpty();
        RuleFor(x => x.NewIssuedOn).NotEqual(default(DateOnly));
        RuleFor(x => x.NewExpiresOn).GreaterThan(x => x.NewIssuedOn).When(x => x.NewExpiresOn.HasValue);
    }
}


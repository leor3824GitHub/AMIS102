using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.DeleteAccountability;

public sealed class DeleteAccountabilityCommandValidator : AbstractValidator<DeleteAccountabilityCommand>
{
    public DeleteAccountabilityCommandValidator()
    {
        RuleFor(x => x.AccountabilityId).NotEmpty();
    }
}

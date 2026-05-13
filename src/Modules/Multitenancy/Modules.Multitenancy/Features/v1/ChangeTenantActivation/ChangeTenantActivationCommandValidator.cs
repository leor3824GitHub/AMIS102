using FluentValidation;
using AMIS.Modules.Multitenancy.Contracts.v1.ChangeTenantActivation;

namespace AMIS.Modules.Multitenancy.Features.v1.ChangeTenantActivation;

internal sealed class ChangeTenantActivationCommandValidator : AbstractValidator<ChangeTenantActivationCommand>
{
    public ChangeTenantActivationCommandValidator() =>
       RuleFor(t => t.TenantId)
           .NotEmpty();
}

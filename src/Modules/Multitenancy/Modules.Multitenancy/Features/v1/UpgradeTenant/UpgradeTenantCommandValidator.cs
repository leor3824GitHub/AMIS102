using FluentValidation;
using AMIS.Modules.Multitenancy.Contracts.v1.UpgradeTenant;

namespace AMIS.Modules.Multitenancy.Features.v1.UpgradeTenant;

public sealed class UpgradeTenantCommandValidator : AbstractValidator<UpgradeTenantCommand>
{
    public UpgradeTenantCommandValidator()
    {
        RuleFor(t => t.Tenant).NotEmpty();
        RuleFor(t => t.ExtendedExpiryDate).GreaterThan(DateTime.UtcNow);
    }
}

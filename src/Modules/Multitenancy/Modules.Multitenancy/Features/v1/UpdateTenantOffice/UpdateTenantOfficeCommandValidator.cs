using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Multitenancy.Contracts.v1.UpdateTenantOffice;
using FluentValidation;

namespace AMIS.Modules.Multitenancy.Features.v1.UpdateTenantOffice;

public sealed class UpdateTenantOfficeCommandValidator : AbstractValidator<UpdateTenantOfficeCommand>
{
    public UpdateTenantOfficeCommandValidator(ITenantService tenantService)
    {
        // Checked against the registry table, not ExistsWithIdAsync: that goes through the Finbuckle store,
        // which is a distributed cache here and only knows tenants an incoming request has already
        // resolved — so it reports a real but untouched tenant as missing.
        RuleFor(t => t.TenantId).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (id, ct) => await tenantService.FindByIdAsync(id, ct).ConfigureAwait(false) is not null)
            .WithMessage((_, id) => $"Tenant {id} does not exist.");

        RuleFor(t => t.OfficeId).Cascade(CascadeMode.Stop)
            .NotEmpty();

        // One office represents exactly one agency; re-linking a tenant to the office it already holds is
        // allowed so the call stays idempotent.
        RuleFor(t => t).Cascade(CascadeMode.Stop)
            .MustAsync(async (command, ct) =>
            {
                var claimant = await tenantService.FindByOfficeIdAsync(command.OfficeId, ct).ConfigureAwait(false);
                return claimant is null
                    || string.Equals(claimant.Id, command.TenantId, StringComparison.OrdinalIgnoreCase);
            })
            .WithMessage("That office is already linked to another tenant.");
    }
}

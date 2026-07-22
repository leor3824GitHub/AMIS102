using FluentValidation;
using AMIS.Framework.Persistence;
using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Multitenancy.Contracts.v1.CreateTenant;

namespace AMIS.Modules.Multitenancy.Features.v1.CreateTenant;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator(ITenantService tenantService, IConnectionStringValidator connectionStringValidator)
    {
        RuleFor(t => t.Id).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (id, ct) => !await tenantService.ExistsWithIdAsync(id, ct).ConfigureAwait(false))
            .WithMessage((_, id) => $"Tenant {id} already exists.");

        RuleFor(t => t.Name).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (name, ct) => !await tenantService.ExistsWithNameAsync(name!, ct).ConfigureAwait(false))
            .WithMessage((_, name) => $"Tenant {name} already exists.");

        RuleFor(t => t.ConnectionString).Cascade(CascadeMode.Stop)
            .Must((_, cs) => string.IsNullOrWhiteSpace(cs) || connectionStringValidator.TryValidate(cs))
            .WithMessage("Connection string invalid.");

        RuleFor(t => t.AdminEmail).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();

        // A unique index enforces this at the database too, but catching it here turns a 500 into a
        // readable validation message. One office can only ever represent one agency, otherwise resolving
        // a recipient employee to a destination tenant would be ambiguous.
        RuleFor(t => t.OfficeId).Cascade(CascadeMode.Stop)
            .MustAsync(async (officeId, ct) =>
                !officeId.HasValue
                || officeId.Value == Guid.Empty
                || await tenantService.FindByOfficeIdAsync(officeId.Value, ct).ConfigureAwait(false) is null)
            .WithMessage("That office is already linked to another tenant.");
    }
}

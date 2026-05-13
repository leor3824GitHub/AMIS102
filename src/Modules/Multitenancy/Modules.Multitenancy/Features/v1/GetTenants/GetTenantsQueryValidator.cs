using FluentValidation;
using AMIS.Framework.Web.Validation;
using AMIS.Modules.Multitenancy.Contracts.v1.GetTenants;

namespace AMIS.Modules.Multitenancy.Features.v1.GetTenants;

public sealed class GetTenantsQueryValidator : AbstractValidator<GetTenantsQuery>
{
    public GetTenantsQueryValidator()
    {
        Include(new PagedQueryValidator<GetTenantsQuery>());
    }
}


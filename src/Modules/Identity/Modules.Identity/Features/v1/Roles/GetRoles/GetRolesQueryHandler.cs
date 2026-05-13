using AMIS.Modules.Identity.Contracts.DTOs;
using AMIS.Modules.Identity.Contracts.Services;
using AMIS.Modules.Identity.Contracts.v1.Roles.GetRoles;
using Mediator;

namespace AMIS.Modules.Identity.Features.v1.Roles.GetRoles;

public sealed class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, IEnumerable<RoleDto>>
{
    private readonly IRoleService _roleService;

    public GetRolesQueryHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async ValueTask<IEnumerable<RoleDto>> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        return await _roleService.GetRolesAsync(cancellationToken).ConfigureAwait(false);
    }
}


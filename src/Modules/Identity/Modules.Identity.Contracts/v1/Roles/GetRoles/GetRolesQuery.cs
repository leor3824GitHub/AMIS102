using AMIS.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace AMIS.Modules.Identity.Contracts.v1.Roles.GetRoles;

public sealed record GetRolesQuery : IQuery<IEnumerable<RoleDto>>;



using AMIS.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace AMIS.Modules.Identity.Contracts.v1.Roles.GetRoleWithPermissions;

public sealed record GetRoleWithPermissionsQuery(string Id) : IQuery<RoleDto>;



using AMIS.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace AMIS.Modules.Identity.Contracts.v1.Users.GetUsers;

public sealed record GetUsersQuery : IQuery<List<UserDto>>;



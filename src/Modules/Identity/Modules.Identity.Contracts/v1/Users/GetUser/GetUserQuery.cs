using AMIS.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace AMIS.Modules.Identity.Contracts.v1.Users.GetUser;

public sealed record GetUserQuery(string Id) : IQuery<UserDto>;



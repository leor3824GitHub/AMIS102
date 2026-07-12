using AMIS.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace AMIS.Modules.Identity.Contracts.v1.Users.GetUsers;

/// <summary>
/// Resolves many users by id in one round-trip. Backs the "by-ids" batch endpoint that replaces
/// fetching the entire tenant user list just to display a handful of linked accounts (e.g. EmployeesPage).
/// Returns only the ids that exist; unknown ids are simply absent from the result.
/// </summary>
public sealed record GetUsersByIdsQuery(IReadOnlyCollection<string> UserIds) : IQuery<List<UserDto>>;

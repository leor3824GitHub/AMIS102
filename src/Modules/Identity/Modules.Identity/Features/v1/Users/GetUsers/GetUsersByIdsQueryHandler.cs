using AMIS.Modules.Identity.Contracts.DTOs;
using AMIS.Modules.Identity.Contracts.Services;
using AMIS.Modules.Identity.Contracts.v1.Users.GetUsers;
using Mediator;

namespace AMIS.Modules.Identity.Features.v1.Users.GetUsers;

public sealed class GetUsersByIdsQueryHandler(IUserService userService)
    : IQueryHandler<GetUsersByIdsQuery, List<UserDto>>
{
    public async ValueTask<List<UserDto>> Handle(GetUsersByIdsQuery query, CancellationToken cancellationToken)
        => await userService.GetByIdsAsync(query.UserIds, cancellationToken).ConfigureAwait(false);
}

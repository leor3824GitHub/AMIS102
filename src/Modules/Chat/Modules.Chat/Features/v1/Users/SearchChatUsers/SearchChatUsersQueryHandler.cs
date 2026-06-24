using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Contracts.v1.Users;
using AMIS.Modules.Identity.Contracts.DTOs;
using AMIS.Modules.Identity.Contracts.Services;
using Mediator;

namespace AMIS.Modules.Chat.Features.v1.Users.SearchChatUsers;

/// <summary>
/// Returns up to 20 active, mentionable users matching the search term (over the identity directory via
/// <see cref="IUserService"/>). Filters in memory — the same trade-off as the mention resolver, acceptable
/// at this scale. <c>UserDto.Id</c>/<c>UserName</c> are nullable, so they are null-filtered first.
/// </summary>
public sealed class SearchChatUsersQueryHandler : IQueryHandler<SearchChatUsersQuery, IReadOnlyList<ChatUserDto>>
{
    private const int MaxResults = 20;

    private readonly IUserService _userService;

    public SearchChatUsersQueryHandler(IUserService userService) => _userService = userService;

    public async ValueTask<IReadOnlyList<ChatUserDto>> Handle(SearchChatUsersQuery query, CancellationToken cancellationToken)
    {
        var term = query.Query?.Trim();
        var users = await _userService.GetListAsync(cancellationToken).ConfigureAwait(false);

        return users
            .Where(u => u.Id is not null && u.UserName is not null && u.IsActive)
            .Where(u => string.IsNullOrWhiteSpace(term)
                        || u.UserName!.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || DisplayName(u).Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(u => new ChatUserDto(u.Id!, u.UserName!, DisplayName(u)))
            .OrderBy(u => u.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(MaxResults)
            .ToList();
    }

    private static string DisplayName(UserDto user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return !string.IsNullOrWhiteSpace(fullName) ? fullName : user.UserName!;
    }
}

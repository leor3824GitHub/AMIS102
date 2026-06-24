using AMIS.Modules.Chat.Contracts.v1.DTOs;
using Mediator;

namespace AMIS.Modules.Chat.Contracts.v1.Users;

/// <summary>
/// Search the directory of mentionable users for the mention picker. Returns active users whose username or
/// full name matches <paramref name="Query"/> (empty returns the first page). Exposed to chat users without
/// the user-management permission, so the Blazor mention picker doesn't depend on the admin user directory.
/// </summary>
public sealed record SearchChatUsersQuery(string? Query = null) : IQuery<IReadOnlyList<ChatUserDto>>;

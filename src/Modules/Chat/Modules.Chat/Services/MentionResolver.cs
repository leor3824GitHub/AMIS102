using AMIS.Modules.Identity.Contracts.Services;

namespace AMIS.Modules.Chat.Services;

/// <summary>A <c>@username</c> that resolved to a real, active user.</summary>
public sealed record ResolvedMention(string UserId, string UserName);

public interface IMentionResolver
{
    Task<IReadOnlyList<ResolvedMention>> ResolveAsync(IReadOnlyCollection<string> usernames, CancellationToken cancellationToken);

    /// <summary>Resolves a single user's display name (full name, falling back to username), or null if unknown.</summary>
    Task<string?> ResolveDisplayNameAsync(string userId, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves parsed <c>@username</c> tokens to user ids over the identity directory. Pulls the full user list
/// and filters in memory (upstream's documented trade-off; acceptable at this scale). <c>UserDto.Id</c> and
/// <c>UserName</c> are nullable, so they are null-filtered before matching.
/// </summary>
internal sealed class MentionResolver(IUserService userService) : IMentionResolver
{
    public async Task<IReadOnlyList<ResolvedMention>> ResolveAsync(
        IReadOnlyCollection<string> usernames,
        CancellationToken cancellationToken)
    {
        if (usernames.Count == 0)
        {
            return [];
        }

        var wanted = new HashSet<string>(usernames, StringComparer.OrdinalIgnoreCase);
        var users = await userService.GetListAsync(cancellationToken).ConfigureAwait(false);

        return users
            .Where(u => u.Id is not null && u.UserName is not null && u.IsActive && wanted.Contains(u.UserName))
            .Select(u => new ResolvedMention(u.Id!, u.UserName!))
            .DistinctBy(r => r.UserId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<string?> ResolveDisplayNameAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        try
        {
            var user = await userService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return !string.IsNullOrWhiteSpace(fullName) ? fullName : user.UserName;
        }
        catch (Exception)
        {
            // Directory lookup failed (e.g., user not found) — fall back to no display name.
            return null;
        }
    }
}

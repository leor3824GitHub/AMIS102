using AMIS.Framework.Web.Realtime;
using AMIS.Modules.Chat.Data;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Services;

/// <summary>
/// Hub adapter: the channels a user belongs to, so <c>AppHub.OnConnectedAsync</c> can pre-join their groups.
/// Always appends the global channel id (implicit membership for every authenticated user). Querying
/// membership by user id is inherently tenant-scoped (a user's rows only point to their office's channels).
/// </summary>
internal sealed class UserChannelLookup(ChatDbContext dbContext) : IUserChannelLookup
{
    public async Task<IReadOnlyCollection<Guid>> GetChannelIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var uid = userId.ToString();
        var ids = await dbContext.ChannelMembers
            .Where(m => m.UserId == uid)
            .Select(m => m.ChannelId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        ids.Add(ChatModuleConstants.GlobalChannelId);
        return ids;
    }
}

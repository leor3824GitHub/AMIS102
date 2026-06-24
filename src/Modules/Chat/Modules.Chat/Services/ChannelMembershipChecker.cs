using AMIS.Framework.Web.Realtime;
using AMIS.Modules.Chat.Data;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Services;

/// <summary>
/// Hub adapter: decides whether a user may join a channel group. Special-cases the global channel
/// (implicit membership). Membership is queried directly (the boundary), so no ambient tenant is needed.
/// </summary>
internal sealed class ChannelMembershipChecker(ChatDbContext dbContext) : IChannelMembershipChecker
{
    public async Task<bool> IsMemberAsync(Guid channelId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (channelId == ChatModuleConstants.GlobalChannelId)
        {
            return true;
        }

        var uid = userId.ToString();
        return await dbContext.ChannelMembers
            .AnyAsync(m => m.ChannelId == channelId && m.UserId == uid, cancellationToken)
            .ConfigureAwait(false);
    }
}

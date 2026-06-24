using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Chat.Contracts.v1;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Domain;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Internal;

/// <summary>
/// Shared channel authorization for command/query handlers. Non-members get <see cref="NotFoundException"/>
/// (404) — never 403 — so they cannot probe whether a channel exists.
/// </summary>
internal static class ChannelAuthorization
{
    /// <summary>
    /// Loads a channel the caller may access (tracked). Returns the channel for members and for the global
    /// channel (implicit membership); throws 404 otherwise. The channel query filter already hides channels
    /// from other offices.
    /// </summary>
    public static async Task<ChatChannel> RequireMemberAsync(
        ChatDbContext dbContext,
        Guid channelId,
        string userId,
        CancellationToken cancellationToken)
    {
        var channel = await dbContext.Channels
            .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Channel not found.");

        if (channel.Scope == ChannelScope.Global)
        {
            return channel; // implicit membership for every authenticated user
        }

        var isMember = await dbContext.ChannelMembers
            .AnyAsync(m => m.ChannelId == channelId && m.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        return isMember ? channel : throw new NotFoundException("Channel not found.");
    }

    /// <summary>Ensures the caller is an <see cref="ChannelMemberRole.Admin"/> of the channel; otherwise 403.</summary>
    public static async Task RequireAdminAsync(
        ChatDbContext dbContext,
        Guid channelId,
        string userId,
        CancellationToken cancellationToken)
    {
        var isAdmin = await dbContext.ChannelMembers
            .AnyAsync(m => m.ChannelId == channelId && m.UserId == userId && m.Role == ChannelMemberRole.Admin, cancellationToken)
            .ConfigureAwait(false);

        if (!isAdmin)
        {
            throw new ForbiddenException("You must be a channel admin to perform this action.");
        }
    }
}
